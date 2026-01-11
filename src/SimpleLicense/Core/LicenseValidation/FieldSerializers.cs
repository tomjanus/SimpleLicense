/// <summary>
/// Field-level serializers for converting license field values to JSON-compatible formats.
/// 
/// SERIALIZATION ARCHITECTURE:
/// ===========================
/// Field serializers handle custom conversion of field values before JSON serialization.
/// 
/// PURPOSE:
/// - Convert complex types (DateTime, custom objects) to JSON-friendly formats
/// - Apply consistent formatting rules (e.g., ISO 8601 for dates)
/// - Normalize field values for canonical representation
/// - Support custom serialization logic per field
/// 
/// EXAMPLE USE CASES:
/// - ExpiryUtc: Convert DateTime to ISO 8601 UTC string
/// - Arrays/Lists: Format as JSON arrays
/// - Custom objects: Convert to dictionaries or specific formats
/// 
/// EXTENSIBILITY:
/// - Register custom serializers for application-specific fields
/// - Override default serializers by re-registering with same field name
/// </summary>

using System.Globalization;
using SimpleLicense.Core.Utils;

namespace SimpleLicense.Core.LicenseValidation
{
    /// <summary>
    /// Attribute to mark a method as a field serializer that should be auto-discovered.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class FieldSerializerAttribute : Attribute
    {
        /// <summary>
        /// The name of the field this serializer applies to.
        /// </summary>
        public string FieldName { get; }

        public FieldSerializerAttribute(string fieldName)
        {
            FieldName = fieldName;
        }
    }

    /// <summary>
    /// Delegate type for field serializers.
    /// Takes a field value and returns a JSON-serializable object.
    /// Returns null if the value should not be included in JSON output.
    /// </summary>
    public delegate object? FieldSerializer(object? value);

    /// <summary>
    /// Registry of field serializers with auto-discovery support.
    /// Provides default serializers for standard license fields and allows custom serializer registration.
    /// </summary>
    /// <remarks>
    /// Keys are compared using <see cref="StringComparer.OrdinalIgnoreCase"/> to ensure case-insensitive field lookup.
    /// </remarks>
    public static class FieldSerializers
    {
        private static readonly Dictionary<string, FieldSerializer> _serializers = new(StringComparer.OrdinalIgnoreCase);
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets all registered serializers. Initializes default serializers on first access.
        /// </summary>
        public static IReadOnlyDictionary<string, FieldSerializer> All
        {
            get
            {
                EnsureInitialized();
                return _serializers;
            }
        }

        /// <summary>
        /// Registers a serializer for a specific field name.
        /// </summary>
        /// <param name="fieldName">The field name (case-insensitive)</param>
        /// <param name="serializer">The serializer function</param>
        public static void Register(string fieldName, FieldSerializer serializer)
        {
            ArgumentNullException.ThrowIfNull(fieldName);
            ArgumentNullException.ThrowIfNull(serializer);
            
            lock (_lock)
            {
                _serializers[fieldName] = serializer;
            }
        }

        /// <summary>
        /// Tries to get a serializer for a specific field.
        /// </summary>
        public static bool TryGetSerializer(string fieldName, out FieldSerializer? serializer)
        {
            EnsureInitialized();
            return _serializers.TryGetValue(fieldName, out serializer);
        }

        /// <summary>
        /// Gets a serializer for a specific field, or null if not registered.
        /// </summary>
        public static FieldSerializer? GetSerializer(string fieldName)
        {
            EnsureInitialized();
            return _serializers.TryGetValue(fieldName, out var serializer) ? serializer : null;
        }

        /// <summary>
        /// Ensures default serializers are initialized.
        /// Thread-safe lazy initialization.
        /// </summary>
        private static void EnsureInitialized()
        {
            if (_initialized) return;
            
            lock (_lock)
            {
                if (_initialized) return;
                
                RegisterDefaultSerializers();
                _initialized = true;
            }
        }

        /// <summary>
        /// Registers default serializers for standard license fields.
        /// </summary>
        private static void RegisterDefaultSerializers()
        {
            // ExpiryUtc: Convert DateTime to ISO 8601 UTC string
            Register("ExpiryUtc", SerializeExpiryUtc);
            
            // Add more default serializers as needed
        }

        /// <summary>
        /// Serializer for ExpiryUtc field - converts DateTime to ISO 8601 UTC string.
        /// </summary>
        [FieldSerializer("ExpiryUtc")]
        private static object? SerializeExpiryUtc(object? value)
        {
            if (value == null) return null;
            
            if (value is DateTime dt)
            {
                return dt.ToUniversalTime().ToString("o");
            }
            
            if (value is DateTimeOffset dto)
            {
                return dto.ToUniversalTime().ToString("o");
            }
            
            if (value is string str)
            {
                // Already a string, try to parse and re-format to ensure consistency
                if (DateTime.TryParse(str, null, DateTimeStyles.RoundtripKind, out var parsed))
                {
                    return parsed.ToUniversalTime().ToString("o");
                }
                // If can't parse, return as-is
                return str;
            }
            
            // For other types, return as-is
            return value;
        }

        /// <summary>
        /// Clears all registered serializers.
        /// Useful for testing or resetting to defaults.
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _serializers.Clear();
                _initialized = false;
            }
        }

        /// <summary>
        /// Resets serializers to defaults by clearing and re-initializing.
        /// </summary>
        public static void ResetToDefaults()
        {
            lock (_lock)
            {
                _serializers.Clear();
                RegisterDefaultSerializers();
                _initialized = true;
            }
        }
    }
}
