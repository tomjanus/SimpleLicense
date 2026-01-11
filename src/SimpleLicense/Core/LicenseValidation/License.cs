/// LicenseDocument represents a license with mandatory and custom fields,
/// and supports JSON serialization/deserialization.
/// 
/// VALIDATION ARCHITECTURE:
/// ========================
/// LicenseDocument uses FIELD-LEVEL VALIDATION only:
/// - Validates individual field values when they are set
/// - Ensures fields have correct format and type
/// - Uses FieldValidators registry for validation logic
/// 
/// For SCHEMA-LEVEL VALIDATION (structure, required fields, etc.),
/// use the LicenseValidator class instead.
/// 
/// See FieldValidators.cs for detailed validation architecture documentation.

using System.Text;
using System.Text.Json;
using SimpleLicense.Core.Utils;

namespace SimpleLicense.Core.LicenseValidation
{

    /// <summary>
    /// Exception thrown when license validation fails.
    /// Contains a list of issues found.
    /// </summary>
    public class LicenseValidationException : Exception
    {
        public IReadOnlyList<string> Issues { get; }

        public LicenseValidationException(IEnumerable<string>? issues)
            : base(CreateMessage(issues))
        {
            Issues = issues?.ToList() ?? new List<string>();
        }

        private static string CreateMessage(IEnumerable<string>? issues)
        {
            var sb = new StringBuilder();
            sb.AppendLine("License validation failed with the following issue(s):");
            if (issues is null)
            {
                sb.AppendLine(" - (no details provided)");
                return sb.ToString();
            }
            if (issues.Any())
            {
                foreach (var issue in issues)
                {
                    sb.AppendLine($" - {issue}");
                }
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Represents the outcome of a validation operation, including whether it
    /// succeeded, an optional validation output value, and an error message
    /// if validation failed.
    /// </summary>
    public readonly struct ValidationResult
    {
        /// <summary>
        /// Gets a value indicating whether the validation succeeded.
        /// </summary>
        public bool IsValid { get; }
        /// <summary>
        /// Gets the value produced by the validator when validation succeeds.
        /// This may contain a transformed or normalized value. When validation
        /// fails, this value is typically <c>null</c>.
        /// </summary>
        public object? OutputValue { get; }
        /// <summary>
        /// Gets the error message describing why validation failed, or
        /// <c>null</c> when validation succeeds.
        /// </summary>
        public string? Error { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationResult"/> struct.
        /// </summary>
        /// <param name="isValid">Indicates whether validation was successful.</param>
        /// <param name="outputValue">The resulting value when validation succeeds; otherwise <c>null</c>.</param>
        /// <param name="error">
        /// The error message when validation fails; otherwise <c>null</c>.
        /// </param>
        public ValidationResult(bool isValid, object? outputValue = null, string? error = null)
        {
            IsValid = isValid;
            OutputValue = outputValue;
            Error = error;
        }

        public static ValidationResult Success(object? outputValue) => new(true, outputValue, null);
        public static ValidationResult Fail(string error) => new(false, null, error);
    }

    /// <summary>
    /// Represents a license document with mandatory and custom fields.
    /// Supports field-level validation using the FieldValidators registry.
    /// </summary>
    public class License
    {
        // --- Mandatory field names (every license must contain an Id and Signature) ---
        private static readonly string[] MandatoryFieldNames = [ "LicenseId", "ExpiryUtc", "Signature" ];

        // --- Custom license fields ---
        private readonly Dictionary<string, object?> _fields = new(
            StringComparer.OrdinalIgnoreCase
        );

        /// <summary>
        /// Initializes a new instance of the <see cref="License"/> class.
        /// Validators are automatically loaded from the FieldValidators registry.
        /// </summary>
        /// <param name="ensureMandatoryKeys">If true, ensures mandatory fields exist with null values if not set.</param> 
        public License(bool ensureMandatoryKeys = true)
        {
            // Validators are managed by FieldValidators registry (auto-initialized on first access)
            if (ensureMandatoryKeys)
            {
                foreach (var key in MandatoryFieldNames)
                {
                    if (!_fields.ContainsKey(key))  _fields[key] = null;
                }   
            }
        }

        // --- Public API: a indexer that uses field validation ---
        /// <summary>
        /// Gets or sets a field value by name.
        /// Setting a field runs validation if a validator is registered for that field.
        /// If validation fails, a LicenseValidationException is thrown.
        /// </summary>
        /// <param name="name">The field name (case-insensitive)</param>
        /// <returns>The field value, or null if the field doesn't exist</returns>
        /// <exception cref="LicenseValidationException">Thrown when field validation fails</exception>
        public object? this[string name]
        {
            get => _fields.TryGetValue(name, out var v) ? v : null;
            set
            {
                // Use SetField which validates and normalizes fields
                if (!SetField(name, value, out var error))
                {
                    throw new LicenseValidationException(new[] { $"Field '{name}' validation failed: {error}" });
                }
            }
        }

        /// <summary>
        /// Sets a field value with validation.
        /// If a validator exists in the FieldValidators registry, it is used to validate and normalize the value.
        /// If validation fails, returns false and sets the error message.
        /// If no validator exists, the raw value is stored without validation.
        /// </summary>
        /// <param name="name">The field name (case-insensitive)</param>
        /// <param name="value">The field value to set</param>
        /// <param name="error">Output error message if validation fails</param>
        /// <returns>True if the field was set successfully, false if validation failed</returns>
        public bool SetField(string name, object? value, out string? error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(name))
            {
                error = "Field name cannot be null or whitespace.";
                return false;
            }

            // Check if a validator exists in the registry
            if (FieldValidators.TryGetValidator(name, out var validator) && validator != null)
            {
                var result = validator(value);
                if (!result.IsValid)
                {
                    error = result.Error ?? "validation failed";
                    return false;
                }
                // Store normalized output value (may be null intentionally)
                _fields[name] = result.OutputValue;
                return true;
            }
            // No validator: just store raw value
            _fields[name] = value;
            return true;
        }

        /// <summary>
        /// Convenience property to enumerate all fields in the license.
        /// </summary>
        public IEnumerable<KeyValuePair<string, object?>> Fields => _fields;

        /// <summary>
        /// Ensures mandatory fields exist and have valid values according to their validators.
        /// Collects all validation problems and throws LicenseValidationException if any are found.
        /// 
        /// NOTE: This only validates mandatory fields. For full schema validation,
        /// use LicenseValidator class instead.
        /// </summary>
        /// <exception cref="LicenseValidationException">Thrown when any mandatory field is invalid</exception>
        public void EnsureMandatoryPresent()
        {
            var issues = new List<string>();

            foreach (var name in MandatoryFieldNames)
            {
                var fieldValue = this[name];
                
                // Check if validator exists in the registry
                if (FieldValidators.TryGetValidator(name, out var validator) && validator != null)
                {
                    var result = validator(fieldValue);
                    if (!result.IsValid)
                        issues.Add($"Field '{name}' is invalid: {result.Error}");
                }
                else
                {
                    // No validator - just check if field is null
                    if (fieldValue == null)
                        issues.Add($"Mandatory field '{name}' is null");
                }
            }
            
            if (issues.Count > 0)
                throw new LicenseValidationException(issues);
        }

        /// <summary>
        /// Serializes the LicenseDocument to JSON.
        /// Optionally validates mandatory fields before serialization.
        /// </summary>
        /// <param name="validate">If true, runs EnsureMandatoryPresent before serialization.</param>
        /// <returns>JSON string representation of the license</returns>
        public string ToJson(bool validate = true)
        {
            if (validate) EnsureMandatoryPresent();

            var outDict = new Dictionary<string, object?>(_fields, StringComparer.OrdinalIgnoreCase);

            // If ExpiryUtc is a DateTime, produce ISO string for JSON consumers
            if (outDict.TryGetValue("ExpiryUtc", out var expiry) && expiry is DateTime dt)
            {
                outDict["ExpiryUtc"] = dt.ToUniversalTime().ToString("o");
            }

            try
            {
                return JsonSerializer.Serialize(outDict, new JsonSerializerOptions { WriteIndented = true });
            } 
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to serialize LicenseDocument to JSON.", ex);
            }
        }

        /// <summary>
        /// Deserializes a LicenseDocument from JSON.
        /// Uses SetField to apply validation during deserialization.
        /// </summary>
        /// <param name="json">JSON string representation of the license</param>
        /// <returns>A new LicenseDocument instance</returns>
        /// <exception cref="LicenseValidationException">Thrown when deserialization or validation fails</exception>
        public static License FromJson(string json)
        {
            Dictionary<string, JsonElement> parsed;
            try
            {
                parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)
                            ?? new Dictionary<string, JsonElement>();
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Failed to parse LicenseDocument JSON.", ex);
            }
            
            var doc = new License(ensureMandatoryKeys: false);
            var errors = new List<string>();

            foreach (var kv in parsed)
            {
                object? value;
                try 
                {
                    value = JsonElementToClr(kv.Value);
                } 
                catch (Exception ex)
                {
                    errors.Add($"Field '{kv.Key}': failed to parse value - {ex.Message}");
                    break;
                }

                // Delegate setting and validation to SetField
                if (!doc.SetField(kv.Key, value, out var err))
                {
                    errors.Add($"{kv.Key}: {err}");
                }
            }
            
            // Ensure mandatory keys exist (null if missing)
            foreach (var k in MandatoryFieldNames)
            {
                if (!doc._fields.ContainsKey(k)) doc._fields[k] = null;
            }
            
            if (errors.Count > 0)
            {
                throw new LicenseValidationException(errors);
            }
            
            return doc;
        }

        /// <summary>
        /// Registers a custom validator for a specific field.
        /// This adds or overrides a validator in the global FieldValidators registry.
        /// </summary>
        /// <param name="fieldName">The field name (case-insensitive)</param>
        /// <param name="validator">The validator function</param>
        /// <exception cref="ArgumentException">Thrown when fieldName is null or whitespace</exception>
        /// <exception cref="ArgumentNullException">Thrown when validator is null</exception>
        public static void RegisterFieldValidator(string fieldName, FieldValidator validator)
        {
            FieldValidators.Register(fieldName, validator);
        }

        /// <summary>
        /// Converts a JsonElement to a CLR object.
        /// </summary>
        private static object? JsonElementToClr(JsonElement el) =>
            el.ValueKind switch
            {
                JsonValueKind.String => el.GetString(),
                JsonValueKind.Number => el.TryGetInt64(out var i) ? (object)i : el.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Array => el.EnumerateArray().Select(JsonElementToClr).ToList(),
                JsonValueKind.Object => el.EnumerateObject().ToDictionary(p => p.Name, p => JsonElementToClr(p.Value)),
                _ => el.GetRawText()
            };
    }
}


