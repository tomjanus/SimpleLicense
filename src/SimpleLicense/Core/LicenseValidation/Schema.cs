/// <summary>
/// Schema related classes and utilities. 
/// Schemas are used to define the structure and check if the license contains
/// all the required fields necassary to define a valid license.
/// </summary>

using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;


namespace SimpleLicense.Core.LicenseValidation
{
    /// <summary>
    /// Describes a single field in a license schema, including
    /// its name, data type, validation requirements, and default value (optional).
    /// Each field represents a piece of information that can be included in a license.
    /// </summary>
    public sealed record FieldDescriptor
    {
        /// <summary>
        /// The logical name of the field as it appears in the license.
        /// </summary>
        public string Name { get; init; } = string.Empty;
        
        /// <summary>
        /// The data type of the field (for example: "string", "int",
        /// "datetime", "list&lt;string&gt;", or "bool").
        /// </summary>
        public string Type { get; init; } = string.Empty;
        
        /// <summary>
        /// Indicates whether the field value is included in the license
        /// signature and therefore protected against tampering.
        /// </summary>
        public bool Signed { get; init; } = true;
        
        /// <summary>
        /// Indicates whether the field must be present in every license
        /// that conforms to this schema.
        /// </summary>
        public bool Required { get; init; } = false;
        
        /// <summary>
        /// The default value that should be applied when the field is
        /// not explicitly provided in the license payload.
        /// </summary>
        public object? DefaultValue { get; init; } = null;

        /// <summary>
        /// Optional processor name to apply during license creation.
        /// The processor transforms the input value before validation and serialization.
        /// Example: "HashFiles" to compute file hashes, "GenerateGuid" to auto-generate IDs.
        /// </summary>
        public string? Processor { get; init; } = null;

        /// <summary>
        /// Parameterless constructor for deserialization.
        /// </summary>
        public FieldDescriptor()
        {
        }

        /// <summary>
        /// Creates a new field descriptor with the specified parameters.
        /// </summary>
        public FieldDescriptor(
            string Name, 
            string Type, 
            bool Signed = true, 
            bool Required = false, 
            object? DefaultValue = null,
            string? Processor = null)
        {
            this.Name = Name;
            this.Type = Type;
            this.Signed = Signed;
            this.Required = Required;
            this.DefaultValue = DefaultValue;
            this.Processor = Processor;
        }
    };

    /// <summary>
    /// Represents a license schema, consisting of a schema name and
    /// a collection of field descriptors that define the allowed
    /// structure of license data.
    /// </summary>
    public sealed record LicenseSchema
    {
        /// <summary>
        /// The name of this schema.
        /// </summary>
        public string Name { get; init; } = string.Empty;
        
        /// <summary>
        /// The list of field descriptors that define the structure of licenses conforming to this schema.
        /// </summary>
        public List<FieldDescriptor> Fields { get; init; } = new();

        /// <summary>
        /// Parameterless constructor for deserialization.
        /// </summary>
        public LicenseSchema()
        {
        }

        /// <summary>
        /// Creates a new license schema with the specified name and fields.
        /// </summary>
        /// <param name="name">The name of the schema.</param>
        /// <param name="fields">The field descriptors that define the schema structure.</param>
        public LicenseSchema(string name, List<FieldDescriptor> fields)
        {
            Name = name;
            Fields = fields;
        }

        /// <summary>
        /// Retrieves the field descriptor for the specified field name.
        /// </summary>
        /// <param name="fieldName">The name of the field to retrieve.</param>
        /// <returns>
        /// The <see cref="FieldDescriptor"/> for the specified field name,
        /// or null if the field is not found in the schema.
        /// </returns>
        public FieldDescriptor? GetFieldDesciptor(string fieldName)
        {
            return Fields.FirstOrDefault(f => f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
        }

        private static string DetectFormat(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext is ".json") return "json";
            if (ext is ".yml" or ".yaml") return "yaml";
            // Fallback: sniff content
            var content = File.ReadAllText(path).TrimStart();
            if (content.StartsWith("{") || content.StartsWith("["))
                return "json";
            return "yaml";
        }

        /// <summary>
        /// Loads a license schema from a file, automatically detecting
        /// whether it is in JSON or YAML format based on the file extension
        /// or content.
        /// </summary>
        /// <param name="path">The file path to load the schema from.</param>
        /// <returns>
        /// A <see cref="LicenseSchema"/> instance created from the file content.
        /// </returns>
        /// <exception cref="FileNotFoundException">
        /// Thrown when the specified file does not exist.
        /// </exception>
        public static LicenseSchema FromFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Schema file not found.", path);

            var format = DetectFormat(path);
            var text = File.ReadAllText(path);

            return format switch
            {
                "json" => FromJson(text),
                "yaml" => FromYaml(text),
                _      => throw new InvalidOperationException($"Unsupported schema format: {format}")
            };
        }

        /// <summary>
        /// Saves the license schema to a file in either JSON or YAML format,
        /// based on the file extension.
        /// </summary>
        /// <param name="schema">The schema to save.</param>
        /// <param name="path">The file path to save the schema to.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the provided schema is null.
        /// </exception>
        public static void ToFile(LicenseSchema schema, string path)
        {
            ArgumentNullException.ThrowIfNull(schema);
            var ext = Path.GetExtension(path).ToLowerInvariant();
            string text = ext switch
            {
                ".json" => ToJson(schema),
                ".yaml" or ".yml" => ToYaml(schema),
                _ => throw new InvalidOperationException(
                    $"Unsupported file extension '{ext}'. Use .json, .yaml, or .yml.")
            };
            File.WriteAllText(path, text);
        }

        // -------- JSON ----------

        /// <summary>
        /// Deserializes a JSON document into a <see cref="LicenseSchema"/>.
        /// </summary>
        /// <param name="json">The JSON text representing a schema.</param>
        /// <returns>
        /// A <see cref="LicenseSchema"/> instance created from the JSON content.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the JSON cannot be parsed into a valid schema.
        /// </exception>
        public static LicenseSchema FromJson(string json) =>
            JsonSerializer.Deserialize<LicenseSchema>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("Could not parse license schema JSON.");

        /// <summary>
        /// Serializes the specified schema to its JSON representation.
        /// </summary>
        /// <param name="schema">The schema to serialize.</param>
        /// <returns>
        /// A formatted JSON string that represents the provided schema.
        /// </returns>
        public static string ToJson(LicenseSchema schema)
        {
            ArgumentNullException.ThrowIfNull(schema);
            return JsonSerializer.Serialize(schema, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
        // -------- YAML ----------

        /// <summary>
        /// Serializes the specified schema to its YAML representation.
        /// </summary>
        /// <param name="schema">The schema to serialize.</param>
        /// <returns>
        /// A formatted YAML string that represents the provided schema.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the provided schema is null.
        /// </exception>
        public static LicenseSchema FromYaml(string yaml)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            return deserializer.Deserialize<LicenseSchema>(yaml)
                ?? throw new InvalidOperationException("Could not parse license schema YAML.");
        }

        /// <summary>
        /// Serializes the specified schema to its YAML representation.
        /// </summary>
        /// <param name="schema">The schema to serialize.</param>
        /// <returns>
        /// A formatted YAML string that represents the provided schema.
        /// </returns>
        public static string ToYaml(LicenseSchema schema)
        {
            ArgumentNullException.ThrowIfNull(schema);
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                .Build();
            return serializer.Serialize(schema);
        }

        /// <summary>
        /// Validates the schema and throws an exception if it is invalid.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the schema is invalid, with details about the validation errors.
        /// </exception>
        public void ValidateItself()
        {
            var errors = ValidateCore();
            if (errors.Count > 0)
                throw new InvalidOperationException(
                    $"Schema validation failed:{Environment.NewLine} - " +
                    string.Join(Environment.NewLine + " - ", errors));
        }

        /// <summary>
        /// Validates the schema and returns whether it is valid,
        /// along with a list of validation errors if any.
        /// </summary>
        /// <param name="errors"></param>
        /// <returns>
        /// True if the schema is valid; otherwise, false.
        /// </returns>
        public bool TryValidateItself(out IReadOnlyList<string> errors)
        {
            var list = ValidateCore();
            errors = list;
            return list.Count == 0;
        }

        /// <summary>
        /// Core validation routine used by both Validate() and TryValidate().
        /// Returns a list of human-readable errors (empty if valid).
        /// </summary>
        /// <returns>
        /// A list of validation error messages.
        /// </returns>
        private List<string> ValidateCore()
        {
            var errors = new List<string>();
            // --- Name ---
            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("Schema name must not be empty.");
            // --- Fields ---
            if (Fields is null || Fields.Count == 0)
            {
                errors.Add("Schema must define at least one field.");
                return errors;
            }
            var dupNames = Fields
                .GroupBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (dupNames.Count > 0)
                errors.Add($"Field names must be unique (duplicates: {string.Join(", ", dupNames)}).");
            var allowed = new[]
            {
                "string","int","double","decimal","bool","datetime",
                "list<string>","list<int>","list<double>","list<bool>"
            };
            foreach (var f in Fields)
            {
                if (string.IsNullOrWhiteSpace(f.Name))
                    errors.Add("Every field must have a non-empty Name.");

                if (string.IsNullOrWhiteSpace(f.Type))
                {
                    errors.Add($"Field '{f.Name}': Type must not be empty.");
                    continue;
                }
                if (!allowed.Contains(f.Type.ToLowerInvariant()))
                {
                    errors.Add(
                        $"Field '{f.Name}': Unsupported type '{f.Type}'. " +
                        $"Allowed types: {string.Join(", ", allowed)}");
                }
                if (f.DefaultValue is not null)
                {
                    try { _ = ConvertDefault(f.Type, f.DefaultValue); }
                    catch (Exception ex)
                    {
                        errors.Add(
                            $"Field '{f.Name}': Default value '{f.DefaultValue}' " +
                            $"is incompatible with type '{f.Type}' ({ex.Message}).");
                    }
                }
            }
            return errors;
        }

        private static bool TryGetElements(object value, out IEnumerable<object?> elements)
        {
            elements = Array.Empty<object?>();
            // Already an IEnumerable (but not string-as-text)
            if (value is IEnumerable enumerable && value is not string)
            {
                elements = enumerable.Cast<object?>();
                return true;
            }
            // String that might encode a list
            if (value is string s)
            {
                s = s.Trim();
                // JSON array
                if (s.StartsWith("[") && s.EndsWith("]"))
                {
                    try
                    {
                        var json = JsonSerializer.Deserialize<List<object?>>(s);
                        if (json != null)
                        {
                            elements = json;
                            return true;
                        }
                    }
                    catch
                    {
                        return false;
                    }
                }
                // Optional: CSV fallback
                if (s.Contains(','))
                {
                    elements = s.Split(',')
                                .Select(x => x.Trim())
                                .Cast<object?>();
                    return true;
                }
            }
            return false;
        }

        private static bool TryConvertDefault(
            string type,
            object value,
            out object? result)
        {
            result = null;
            type = type.ToLowerInvariant();

            try
            {
                // ---- Try list parsing first ----
                if (TryGetElements(value, out var elements))
                {
                    var list = new List<object?>();

                    foreach (var item in elements)
                    {
                        if (!TryConvertScalar(type, item!, out var parsed))
                            return false;

                        list.Add(parsed);
                    }

                    result = list;
                    return true;
                }

                // ---- Fallback: scalar ----
                return TryConvertScalar(type, value, out result);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Converts a default value to the specified type.
        /// </summary>
        /// <param name="type">The target type as a string.</param>
        /// <param name="value">The value to convert.</param>
        /// <returns>The converted value.</returns>
        /// <exception cref="InvalidCastException">
        /// Thrown when the specified type is not supported.
        /// </exception>        
        public static object? ConvertDefault(string type, object value)
        {
            if (!TryConvertDefault(type, value, out var result))
                throw new InvalidCastException(
                    $"Failed to convert value '{value}' to type '{type}'.");
            return result!;
        }

        /// <summary>
        /// Converts a default value to the specified type.
        /// </summary>
        /// <param name="type">The target type as a string.</param>
        /// <param name="value">The value to convert.</param>
        /// <returns>The converted value.</returns>
        /// <exception cref="InvalidCastException">
        /// Thrown when the specified type is not supported.
        /// </exception>
        private static bool TryConvertScalar(
            string type,
            object value,
            out object? result)
        {
            result = null;
            try
            {
                result = type switch
                {
                    "string"   => Convert.ToString(value),
                    "int"      => Convert.ToInt32(value),
                    "double"   => Convert.ToDouble(value),
                    "decimal"  => Convert.ToDecimal(value),
                    "bool"     => Convert.ToBoolean(value),
                    "datetime" => DateTime.Parse(Convert.ToString(value)!),
                    _ => throw new InvalidCastException()
                };
                return true;
            }
            catch (Exception ex) when (
                ex is FormatException ||
                ex is InvalidCastException ||
                ex is OverflowException)
            {
                return false;
            }
        }

    }
}
