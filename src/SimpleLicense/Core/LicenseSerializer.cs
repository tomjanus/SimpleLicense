// JSON serialization / deserialization for License and LicenseDocument
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;
using SimpleLicense.Core.Utils;
using SimpleLicense.Core.LicenseValidation;

namespace SimpleLicense.Core
{
    /// <summary>
    /// Central location for all JSON serializer configurations used across the licensing system.
    /// </summary>
    public static class JsonProfiles
    {
        /// <summary>
        /// Canonical serialization used for signing and verifying.
        /// Ensures deterministic output: camelCase, no indentation, explicit nulls.
        /// </summary>
        public static readonly JsonSerializerOptions Canonical = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never, // include explicit nulls
            WriteIndented = false
        };

        /// <summary>
        /// Human-readable pretty-printed JSON.  
        /// Same data rules as Canonical except indentation.
        /// </summary>
        public static readonly JsonSerializerOptions Pretty = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            WriteIndented = true
        };
    }

    /// <summary>
    /// JSON serialization / deserialization for License and LicenseDocument, with configurable options per instance.
    /// </summary>
    public class LicenseSerializer
    {
        /// <summary>
        /// Options used for this serializer instance.
        /// </summary>
        public JsonSerializerOptions Options { get; set; }
        private readonly JsonSerializerOptions _defaultOptions = JsonProfiles.Canonical;

        public LicenseSerializer()
        {
            Options = _defaultOptions;
        }

        /// <summary>
        /// Constructor that allows custom options.
        /// </summary>
        public LicenseSerializer(JsonSerializerOptions? options)
        {
            Options = options ?? _defaultOptions;
        }


        /// <summary>
        /// Serializes a LicenseDocument to JSON string.
        /// Uses the document's built-in ToJson() method if validate is true,
        /// otherwise uses standard JSON serialization.
        /// </summary>
        /// <param name="license">The LicenseDocument to serialize</param>
        /// <param name="validate">If true, validates mandatory fields before serialization</param>
        /// <returns>JSON string representation of the license</returns>
        public string SerializeLicenseDocument(License license, bool validate = false)
        {
            ArgumentNullException.ThrowIfNull(license);
            
            if (validate)
            {
                return license.ToJson(validate: true);
            }
            
            // Use standard JSON serialization for the fields dictionary
            return license.ToJson(validate: false);
        }

        /// <summary>
        /// Deserializes a LicenseDocument from JSON string.
        /// Uses LicenseDocument's built-in FromJson() method which handles field validation.
        /// </summary>
        /// <param name="json">JSON string to deserialize</param>
        /// <returns>A new LicenseDocument instance</returns>
        /// <exception cref="InvalidOperationException">Thrown when JSON parsing fails</exception>
        /// <exception cref="LicenseValidationException">Thrown when field validation fails</exception>
        public License DeserializeLicenseDocument(string json)
        {
            ArgumentNullException.ThrowIfNull(json);
            return License.FromJson(json);
        }

        /// <summary>
        /// Serializes a LicenseDocument to UTF-8 byte array.
        /// </summary>
        public byte[] SerializeLicenseDocumentToBytes(License license, bool validate = false)
        {
            var json = SerializeLicenseDocument(license, validate);
            return Encoding.UTF8.GetBytes(json);
        }

        /// <summary>
        /// Deserializes a LicenseDocument from UTF-8 byte array.
        /// </summary>
        public License DeserializeLicenseDocumentFromBytes(byte[] bytes)
        {
            ArgumentNullException.ThrowIfNull(bytes);
            var json = Encoding.UTF8.GetString(bytes);
            return DeserializeLicenseDocument(json);
        }
    }
}
