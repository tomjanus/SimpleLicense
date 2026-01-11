// Generic license creator supporting schema-based license generation with field processors

using SimpleLicense.Core.LicenseValidation;

namespace SimpleLicense.Core
{
    /// <summary>
    /// Creates licenses based on schemas with support for field processors.
    /// This is a generic, flexible license creator that works with any schema definition.
    /// </summary>
    /// <remarks>
    /// <para>
    /// LicenseCreator is designed to be schema-driven and decoupled from specific use cases.
    /// All license structure and field definitions come from the LicenseSchema, making it
    /// adaptable to any licensing scenario.
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// var creator = new LicenseCreator();
    /// creator.OnInfo += msg => Console.WriteLine(msg);
    /// 
    /// var license = creator.CreateLicense(schema, new Dictionary&lt;string, object?&gt; {
    ///     ["CustomerName"] = "Acme Corp",
    ///     ["ProtectedFiles"] = new[] { "app.exe", "config.dll" }
    /// }, workingDirectory: "/path/to/files");
    /// </code>
    /// </para>
    /// </remarks>
    public class LicenseCreator
    {
        /// <summary>
        /// Event for logging and diagnostic messages during license creation.
        /// Subscribe to receive notifications about processor execution, warnings, etc.
        /// </summary>
        public event Action<string>? OnInfo;
        
        private void Info(string message) => OnInfo?.Invoke(message);

        /// <summary>
        /// Initializes a new instance of the LicenseCreator class.
        /// </summary>
        public LicenseCreator()
        {
        }

        /// <summary>
        /// Creates a License based on a schema, applying field processors and default values.
        /// This is the primary method for creating licenses in a flexible, schema-driven way.
        /// </summary>
        /// <param name="schema">The schema defining the license structure, processors, and validation rules</param>
        /// <param name="fieldValues">Dictionary of field names to raw input values (before processing). 
        /// Fields not provided will use default values from the schema if available.</param>
        /// <param name="workingDirectory">Working directory for file operations (used by processors like HashFiles). 
        /// Defaults to current directory if not specified.</param>
        /// <param name="processorParameters">Optional parameters to pass to all processors. 
        /// Processors can access these via ProcessorContext.Parameters.</param>
        /// <param name="validateSchema">Whether to validate the license against the schema after processing. 
        /// Set to false when processors transform types (e.g., list&lt;string&gt; to dictionary).
        /// Default is false to support type-transforming processors.</param>
        /// <returns>A new License instance with processed field values</returns>
        /// <exception cref="ArgumentNullException">Thrown when schema or fieldValues is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when schema validation fails or processor is not found</exception>
        /// <exception cref="LicenseValidationException">Thrown when validateSchema is true and validation fails</exception>
        /// <remarks>
        /// <para>
        /// This method processes fields in the following order:
        /// <list type="number">
        /// <item>Load raw input values from fieldValues dictionary</item>
        /// <item>Apply default values for missing fields (from schema)</item>
        /// <item>Execute field processors (if specified in schema)</item>
        /// <item>Set processed values in the license document</item>
        /// <item>Optionally validate against schema (if validateSchema is true)</item>
        /// </list>
        /// </para>
        /// <para>
        /// Field processors can transform input values before they're stored in the license.
        /// For example, the "HashFiles" processor converts a list of file paths into a dictionary
        /// of file hashes. See FieldProcessors class for available built-in processors.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var schema = new LicenseSchema("MyLicense", new List&lt;FieldDescriptor&gt; {
        ///     new("LicenseId", "string", Required: true, Processor: "GenerateGuid"),
        ///     new("ExpiryUtc", "datetime", Required: true),
        ///     new("CustomerName", "string", Required: true, Processor: "ToUpper")
        /// });
        /// 
        /// var creator = new LicenseCreator();
        /// var license = creator.CreateLicense(schema, new Dictionary&lt;string, object?&gt; {
        ///     ["ExpiryUtc"] = "2027-12-31T23:59:59Z",
        ///     ["CustomerName"] = "acme corp"  // Will be converted to "ACME CORP"
        /// });
        /// // LicenseId is auto-generated by GenerateGuid processor
        /// </code>
        /// </example>
        public License CreateLicense(
            LicenseSchema schema,
            Dictionary<string, object?> fieldValues,
            string? workingDirectory = null,
            Dictionary<string, object>? processorParameters = null,
            bool validateSchema = false)
        {
            ArgumentNullException.ThrowIfNull(schema);
            ArgumentNullException.ThrowIfNull(fieldValues);

            // Validate the schema itself
            schema.ValidateItself();
            
            var doc = new License(ensureMandatoryKeys: false);
            var processedFields = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            // Process each field defined in the schema
            foreach (var fieldDesc in schema.Fields)
            {
                object? rawValue = null;
                bool hasValue = fieldValues.TryGetValue(fieldDesc.Name, out rawValue);

                // Apply default value if field not provided and default exists
                if (!hasValue && fieldDesc.DefaultValue != null)
                {
                    rawValue = fieldDesc.DefaultValue;
                    hasValue = true;
                }

                // Apply processor if specified
                object? processedValue = rawValue;
                if (!string.IsNullOrWhiteSpace(fieldDesc.Processor))
                {
                    var processor = FieldProcessors.Get(fieldDesc.Processor);
                    if (processor == null)
                    {
                        throw new InvalidOperationException(
                            $"Processor '{fieldDesc.Processor}' specified for field '{fieldDesc.Name}' is not registered. " +
                            $"Available processors: {string.Join(", ", FieldProcessors.All.Keys)}");
                    }

                    var context = new ProcessorContext
                    {
                        FieldName = fieldDesc.Name,
                        FieldDescriptor = fieldDesc,
                        WorkingDirectory = workingDirectory,
                        Parameters = processorParameters ?? new()
                    };

                    try
                    {
                        processedValue = processor(rawValue, context);
                        Info($"Processed field '{fieldDesc.Name}' using processor '{fieldDesc.Processor}'");
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            $"Processor '{fieldDesc.Processor}' failed for field '{fieldDesc.Name}': {ex.Message}", 
                            ex);
                    }
                }

                // Store the processed value
                processedFields[fieldDesc.Name] = processedValue;
            }

            // Set all processed fields in the license document
            foreach (var kvp in processedFields)
            {
                if (!doc.SetField(kvp.Key, kvp.Value, out var error))
                {
                    Info($"Warning: Field '{kvp.Key}' validation failed: {error}");
                }
            }

            // Optionally validate that required fields are present
            // Note: Schema validation may fail if processors transform types (e.g., list<string> -> dictionary)
            if (validateSchema)
            {
                var validator = new LicenseValidator(schema);
                if (!validator.Validate(doc, out var errors))
                {
                    throw new LicenseValidationException(errors.ToArray());
                }
            }

            return doc;
        }

        /// <summary>
        /// Creates a License based on a schema file, applying field processors and default values.
        /// This is a convenience method that loads the schema from a file before creating the license.
        /// </summary>
        /// <param name="schemaPath">Path to the schema file (JSON or YAML format)</param>
        /// <param name="fieldValues">Dictionary of field names to raw input values (before processing)</param>
        /// <param name="workingDirectory">Working directory for file operations (used by processors)</param>
        /// <param name="processorParameters">Optional parameters to pass to processors</param>
        /// <param name="validateSchema">Whether to validate the license against the schema after processing</param>
        /// <returns>A new License instance with processed field values</returns>
        /// <exception cref="FileNotFoundException">Thrown when schema file is not found</exception>
        /// <exception cref="ArgumentNullException">Thrown when schemaPath or fieldValues is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when schema is invalid or processor fails</exception>
        /// <remarks>
        /// The schema file format is automatically detected based on file extension:
        /// <list type="bullet">
        /// <item>.json - JSON format</item>
        /// <item>.yaml or .yml - YAML format</item>
        /// </list>
        /// If extension is ambiguous, the file content is examined to determine format.
        /// </remarks>
        /// <example>
        /// <code>
        /// var creator = new LicenseCreator();
        /// var license = creator.CreateLicenseFromFile(
        ///     "schemas/file_protection.yaml",
        ///     new Dictionary&lt;string, object?&gt; {
        ///         ["CustomerName"] = "Acme Corp",
        ///         ["ProtectedFiles"] = new[] { "app.exe", "data.dll" }
        ///     },
        ///     workingDirectory: "/app/bin"
        /// );
        /// </code>
        /// </example>
        public License CreateLicenseFromFile(
            string schemaPath,
            Dictionary<string, object?> fieldValues,
            string? workingDirectory = null,
            Dictionary<string, object>? processorParameters = null,
            bool validateSchema = false)
        {
            var schema = LicenseSchema.FromFile(schemaPath);
            return CreateLicense(schema, fieldValues, workingDirectory, processorParameters, validateSchema);
        }
    }
}