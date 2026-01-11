/// <summary>
/// Field-level validators for license fields.
/// 
/// VALIDATION ARCHITECTURE:
/// ========================
/// SimpleLicense uses a two-tier validation system of license documents:
/// 
/// 1. FIELD-LEVEL VALIDATION (this file):
///    - Validates individual field VALUES
///    - Handles type conversion and normalization
///    - Ensures field values are in correct format
///    - Example: "ExpiryUtc" must be a valid datetime, "LicenseId" must be non-empty string
///    - Applied when: Setting a field value in LicenseDocument
///    
/// 2. SCHEMA-LEVEL VALIDATION (LicenseValidator.cs):
///    - Validates license STRUCTURE against a schema
///    - Ensures required fields are present
///    - Verifies field types match schema definitions
///    - Checks overall license conforms to expected structure
///    - Applied when: Validating a complete license against a schema
///    
/// SEPARATION OF CONCERNS:
/// - Field validators: "Is this value valid for this field?"
/// - Schema validator: "Does this license have all required fields with correct types?"
/// 
/// EXAMPLE:
/// - Field validator for "MaxUsers": Ensures the value is a positive integer
/// - Schema validator: Ensures "MaxUsers" field exists (if required) and is of type "int"
/// </summary>
/// 
using System.Text.Json;
using System.Globalization;
using NodaTime;
using NodaTime.Text;
using SimpleLicense.Core.Utils;

namespace SimpleLicense.Core.LicenseValidation
{
    /// <summary>
    /// Attribute to mark a method as a field validator that should be auto-discovered.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class FieldValidatorAttribute : Attribute
    {
        /// <summary>
        /// The name of the field this validator applies to.
        /// </summary>
        public string FieldName { get; }

        public FieldValidatorAttribute(string fieldName)
        {
            FieldName = fieldName;
        }
    }

    /// <summary>
    /// Delegate type for field validators.
    /// Takes a field value and returns a ValidationResult indicating success/failure.
    /// </summary>
    public delegate ValidationResult FieldValidator(object? value);

    /// <summary>
    /// Registry of field validators with auto-discovery support.
    /// Provides default validators for standard license fields and allows custom validator registration.
    /// </summary>
    /// <remarks>
    /// Keys are compared using <see cref="StringComparer.OrdinalIgnoreCase"/> to ensure case-insensitive field lookup.
    /// </remarks>
    public static class FieldValidators
    {
        private static readonly Dictionary<string, FieldValidator> _validators = new(StringComparer.OrdinalIgnoreCase);
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets all registered validators. Initializes default validators on first access.
        /// </summary>
        public static IReadOnlyDictionary<string, FieldValidator> All
        {
            get
            {
                EnsureInitialized();
                return _validators;
            }
        }

        /// <summary>
        /// Registers a validator for a specific field name.
        /// </summary>
        /// <param name="fieldName">The field name (case-insensitive)</param>
        /// <param name="validator">The validator function</param>
        public static void Register(string fieldName, FieldValidator validator)
        {
            ArgumentNullException.ThrowIfNull(fieldName);
            ArgumentNullException.ThrowIfNull(validator);
            
            lock (_lock)
            {
                _validators[fieldName] = validator;
            }
        }

        /// <summary>
        /// Tries to get a validator for a specific field.
        /// </summary>
        public static bool TryGetValidator(string fieldName, out FieldValidator? validator)
        {
            EnsureInitialized();
            return _validators.TryGetValue(fieldName, out validator);
        }

        /// <summary>
        /// Gets a validator for a specific field, or null if not registered.
        /// </summary>
        public static FieldValidator? GetValidator(string fieldName)
        {
            EnsureInitialized();
            return _validators.TryGetValue(fieldName, out var validator) ? validator : null;
        }

        /// <summary>
        /// Ensures default validators are registered. Thread-safe.
        /// </summary>
        private static void EnsureInitialized()
        {
            if (_initialized) return;
            lock (_lock)
            {
                if (_initialized) return;
                AutoDiscoverValidators();
                _initialized = true;
            }
        }

        /// <summary>
        /// Auto-discovers validators marked with [FieldValidator] attribute.
        /// </summary>
        private static void AutoDiscoverValidators()
        {
            var methods = typeof(FieldValidators)
                .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Where(m => m.GetCustomAttributes(typeof(FieldValidatorAttribute), false).Length > 0);

            foreach (var method in methods)
            {
                var attr = (FieldValidatorAttribute)method.GetCustomAttributes(typeof(FieldValidatorAttribute), false)[0];
                if (method.ReturnType == typeof(ValidationResult) && 
                    method.GetParameters().Length == 1 && 
                    method.GetParameters()[0].ParameterType == typeof(object))
                {
                    var validator = (FieldValidator)Delegate.CreateDelegate(typeof(FieldValidator), method);
                    Register(attr.FieldName, validator);
                }
            }
        }

        // ============================================================================
        // VALIDATORS
        // ============================================================================

        /// <summary>
        /// Validates LicenseId field: must be a non-empty string.
        /// </summary>
        [FieldValidator("LicenseId")]
        public static ValidationResult ValidateLicenseId(object? value)
        {
            if (value is null)
                return ValidationResult.Fail("LicenseId is required and cannot be null");

            if (value is string s)
            {
                if (string.IsNullOrWhiteSpace(s))
                    return ValidationResult.Fail("LicenseId cannot be empty or whitespace");
                return ValidationResult.Success(s.Trim());
            }

            // Handle JsonElement (from deserialization)
            if (value is JsonElement je && je.ValueKind == JsonValueKind.String)
            {
                var str = je.GetString();
                if (string.IsNullOrWhiteSpace(str))
                    return ValidationResult.Fail("LicenseId cannot be empty or whitespace");
                return ValidationResult.Success(str);
            }

            return ValidationResult.Fail($"LicenseId must be a string, but was {TypeChecking.DescribeType(value)}");
        }

        /// <summary>
        /// Validates ExpiryUtc field: normalizes various input formats to UTC DateTime.
        /// Accepts: DateTime, DateTimeOffset, numeric values (year/unix timestamp), strings.
        /// </summary>
        [FieldValidator("ExpiryUtc")]
        public static ValidationResult ValidateExpiryUtc(object? value)
        {
            if (value is null)
                return ValidationResult.Fail("ExpiryUtc is required and cannot be null");

            // Already a DateTime
            if (value is DateTime dt)
                return ValidationResult.Success(dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime());

            // DateTimeOffset
            if (value is DateTimeOffset dto)
                return ValidationResult.Success(dto.UtcDateTime);

            // Numeric values: year, unix seconds, or unix milliseconds
            if (TypeChecking.IsNumeric(value, out var numeric))
            {
                // Check if it's a year (1-9999, integer)
                if (numeric >= 1 && numeric <= 9999 && Math.Abs(numeric - Math.Round(numeric)) < double.Epsilon)
                {
                    var year = (int)Math.Round(numeric);
                    var d = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    return ValidationResult.Success(d);
                }

                var asLong = (long)numeric;
                
                // Try unix seconds
                try 
                { 
                    return ValidationResult.Success(DateTimeOffset.FromUnixTimeSeconds(asLong).UtcDateTime); 
                } 
                catch { }
                
                // Try unix milliseconds
                try 
                { 
                    return ValidationResult.Success(DateTimeOffset.FromUnixTimeMilliseconds(asLong).UtcDateTime); 
                } 
                catch { }

                return ValidationResult.Fail($"Numeric value {numeric} could not be interpreted as a valid date/time");
            }

            // String forms: try multiple parsing strategies
            if (value is string s)
            {
                if (TryParseDateTime(s, out var parsedDt))
                    return ValidationResult.Success(parsedDt);
                return ValidationResult.Fail($"String value '{s}' could not be parsed as a valid date/time");
            }

            // JsonElement (from deserialization)
            if (value is JsonElement je)
            {
                if (je.ValueKind == JsonValueKind.String)
                {
                    var str = je.GetString();
                    if (str != null && TryParseDateTime(str, out var parsed))
                        return ValidationResult.Success(parsed);
                    return ValidationResult.Fail("Date/time string could not be parsed");
                }
                
                if (je.ValueKind == JsonValueKind.Number)
                {
                    if (je.TryGetInt64(out var l))
                        return ValidateExpiryUtc(l);
                    if (je.TryGetDouble(out var d))
                        return ValidateExpiryUtc(d);
                }
            }

            return ValidationResult.Fail($"ExpiryUtc must be a date/time value, but was {TypeChecking.DescribeType(value)}");
        }

        /// <summary>
        /// Validates Signature field: can be null (unsigned) or non-empty string.
        /// </summary>
        [FieldValidator("Signature")]
        public static ValidationResult ValidateSignature(object? value)
        {
            // Signature can be null for unsigned licenses
            if (value is null)
                return ValidationResult.Success(null);

            if (value is string s)
            {
                if (string.IsNullOrWhiteSpace(s))
                    return ValidationResult.Fail("Signature cannot be empty or whitespace if provided");
                return ValidationResult.Success(s);
            }

            // Handle JsonElement
            if (value is JsonElement je)
            {
                if (je.ValueKind == JsonValueKind.Null)
                    return ValidationResult.Success(null);
                    
                if (je.ValueKind == JsonValueKind.String)
                {
                    var str = je.GetString();
                    if (string.IsNullOrWhiteSpace(str))
                        return ValidationResult.Fail("Signature cannot be empty or whitespace if provided");
                    return ValidationResult.Success(str);
                }
            }

            return ValidationResult.Fail($"Signature must be a string or null, but was {TypeChecking.DescribeType(value)}");
        }

        // ============================================================================
        // HELPER METHODS
        // ============================================================================

        /// <summary>
        /// Attempts to parse a string as a DateTime using multiple strategies.
        /// Uses NodaTime for robust parsing, with fallback to DateTime.TryParse.
        /// </summary>
        private static bool TryParseDateTime(string s, out DateTime result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(s))
                return false;
            
            s = s.Trim();

            // 1. NodaTime Instant (ISO 8601 with timezone)
            var instant = InstantPattern.ExtendedIso.Parse(s);
            if (instant.Success)
            {
                result = instant.Value.ToDateTimeUtc();
                return true;
            }

            // 2. NodaTime OffsetDateTime (ISO 8601 with offset)
            var offsetParse = OffsetDateTimePattern.ExtendedIso.Parse(s);
            if (offsetParse.Success)
            {
                result = offsetParse.Value.ToInstant().ToDateTimeUtc();
                return true;
            }

            // 3. NodaTime LocalDateTime patterns (without timezone)
            var localDateTimePatterns = new[]
            {
                "uuuu-M-d'T'H:m:s",
                "uuuu-M-d H:m:s",
                "M/d/uuuu H:m:s",
                "d/M/uuuu H:m:s"
            };

            foreach (var pattern in localDateTimePatterns)
            {
                var r = LocalDateTimePattern.CreateWithInvariantCulture(pattern).Parse(s);
                if (r.Success)
                {
                    result = r.Value.InZoneLeniently(DateTimeZone.Utc).ToDateTimeUtc();
                    return true;
                }
            }

            // 4. NodaTime LocalDate patterns (date only)
            var localDatePatterns = new[]
            {
                "uuuu-M-d",
                "M/d/uuuu",
                "d/M/uuuu",
                "d-M-uuuu",
                "M-d-uuuu"
            };

            foreach (var pattern in localDatePatterns)
            {
                var r = LocalDatePattern.CreateWithInvariantCulture(pattern).Parse(s);
                if (r.Success)
                {
                    result = r.Value.AtStartOfDayInZone(DateTimeZone.Utc).ToDateTimeUtc();
                    return true;
                }
            }

            // 5. Unix timestamps (numeric strings)
            if (long.TryParse(s, out var numeric))
            {
                try 
                { 
                    result = DateTimeOffset.FromUnixTimeSeconds(numeric).UtcDateTime; 
                    return true; 
                } 
                catch { }
                
                try 
                { 
                    result = DateTimeOffset.FromUnixTimeMilliseconds(numeric).UtcDateTime; 
                    return true; 
                } 
                catch { }
            }

            // 6. Fallback: DateTime.TryParse (handles most formats)
            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, 
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out result))
            {
                return true;
            }

            return false;
        }

        // ============================================================================
        // EXAMPLE CUSTOM VALIDATORS (can be added by users)
        // ============================================================================

        /// <summary>
        /// Example: Validates that MaxUsers is a positive integer.
        /// Mark with [FieldValidator("MaxUsers")] to auto-register.
        /// </summary>
        [FieldValidator("MaxUsers")]
        public static ValidationResult ValidateMaxUsers(object? value)
        {
            if (value is null)
                return ValidationResult.Success(null); // Optional field

            if (TypeChecking.IsNumeric(value, out var num))
            {
                if (num < 0)
                    return ValidationResult.Fail("MaxUsers must be non-negative");
                
                if (num % 1 != 0)
                    return ValidationResult.Fail("MaxUsers must be an integer");

                return ValidationResult.Success((int)num);
            }

            return ValidationResult.Fail($"MaxUsers must be a number, but was {TypeChecking.DescribeType(value)}");
        }

        /// <summary>
        /// Example: Validates that CustomerName is a non-empty string.
        /// </summary>
        [FieldValidator("CustomerName")]
        public static ValidationResult ValidateCustomerName(object? value)
        {
            if (value is null)
                return ValidationResult.Success(null); // Optional unless schema says required

            if (value is string s)
            {
                if (string.IsNullOrWhiteSpace(s))
                    return ValidationResult.Fail("CustomerName cannot be empty or whitespace");
                return ValidationResult.Success(s.Trim());
            }

            return ValidationResult.Fail($"CustomerName must be a string, but was {TypeChecking.DescribeType(value)}");
        }
    }
}
