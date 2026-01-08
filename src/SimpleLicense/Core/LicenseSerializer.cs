// JSON serialization / deserialization for License
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;
using SimpleLicense.Utils;

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
    /// JSON serialization / deserialization for License, with configurable options per instance.
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

        public string Serialize(License license)
        {
            return JsonSerializer.Serialize(license, Options);
        }

        public License Deserialize(string json)
        {
            return JsonSerializer.Deserialize<License>(json, Options)!;
        }
    }
}
