/// This file contains a LicenseValidator class which takes a license object and a schema definition 
/// and validates the license against the schema rules.
/// 
/// VALIDATION ARCHITECTURE:
/// ========================
/// This is the SCHEMA-LEVEL VALIDATOR.
/// 
/// Two-Tier Validation System:
/// 
/// 1. FIELD-LEVEL VALIDATION (FieldValidators.cs):
///    - Validates individual field VALUES
///    - Handles type conversion and normalization
///    - Example: "ExpiryUtc must be a valid datetime", "MaxUsers must be positive"
///    - Applied WHEN: Setting a field value in LicenseDocument
///    
/// 2. SCHEMA-LEVEL VALIDATION (this file):
///    - Validates license STRUCTURE against a schema
///    - Ensures required fields are present
///    - Verifies field types match schema definitions
///    - Example: "CustomerName field is required", "MaxUsers must be type 'int'"
///    - Applied WHEN: Validating a complete license against a schema
///    
/// WHY TWO LEVELS?
/// - Field validators: Ensure data quality (format, range, type)
/// - Schema validator: Ensure structural compliance (completeness, conformance)
/// 
/// EXAMPLE WORKFLOW:
/// 1. User creates a LicenseDocument and sets fields
/// 2. Field validators check each value as it's set (field-level)
/// 3. User validates license against a schema
/// 4. Schema validator checks structure and types (schema-level)

using System.Text;
using System.Text.Json;
using SimpleLicense.Utils;

namespace SimpleLicense.LicenseValidation
{
    /// <summary>
    /// Validates a LicenseDocument against a LicenseSchema to ensure all
    /// required fields are present and have the correct types.
    /// </summary>
    public class LicenseValidator
    {
        private readonly LicenseSchema _schema;

        /// <summary>
        /// Initializes a new instance of the LicenseValidator with the given schema.
        /// </summary>
        /// <param name="schema">The schema to validate licenses against</param>
        public LicenseValidator(LicenseSchema schema)
        {
            ArgumentNullException.ThrowIfNull(schema);
            _schema = schema;
        }

        /// <summary>
        /// Validates a license document against the schema.
        /// Returns true if valid, false otherwise with detailed error information.
        /// </summary>
        /// <param name="license">The license to validate</param>
        /// <param name="errors">Output list of validation errors (empty if valid)</param>
        /// <returns>True if the license conforms to the schema, false otherwise</returns>
        public bool Validate(LicenseDocument license, out List<string> errors)
        {
            ArgumentNullException.ThrowIfNull(license);
            errors = new List<string>();

            // Check each field in the schema
            foreach (var fieldDescriptor in _schema.Fields)
            {
                var fieldName = fieldDescriptor.Name;
                var fieldValue = license[fieldName];

                // Check if required field is missing
                if (fieldDescriptor.Required && fieldValue == null)
                {
                    errors.Add($"Required field '{fieldName}' is missing or null");
                    continue;
                }

                // If field is present, validate its type
                if (fieldValue != null)
                {
                    if (!ValidateFieldType(fieldName, fieldValue, fieldDescriptor.Type, out string? typeError))
                    {
                        errors.Add(typeError!);
                    }
                }
            }

            return errors.Count == 0;
        }

        /// <summary>
        /// Validates a license document and throws an exception if validation fails.
        /// </summary>
        /// <param name="license">The license to validate</param>
        /// <exception cref="LicenseValidationException">Thrown when validation fails</exception>
        public void ValidateOrThrow(LicenseDocument license)
        {
            if (!Validate(license, out var errors))
            {
                throw new LicenseValidationException(errors);
            }
        }

        /// <summary>
        /// Validates that a field value matches the expected type from the schema.
        /// </summary>
        private bool ValidateFieldType(string fieldName, object value, string expectedType, out string? error)
        {
            error = null;
            var normalizedType = expectedType.ToLowerInvariant().Trim();

            switch (normalizedType)
            {
                case "string":
                    if (value is not string)
                    {
                        error = $"Field '{fieldName}' should be string but is {TypeChecking.DescribeType(value)}";
                        return false;
                    }
                    break;

                case "int":
                case "integer":
                    if (!TypeChecking.IsNumeric(value, out double numVal) || numVal % 1 != 0)
                    {
                        error = $"Field '{fieldName}' should be int but is {TypeChecking.DescribeType(value)}";
                        return false;
                    }
                    break;

                case "double":
                case "float":
                case "number":
                    if (!TypeChecking.IsNumeric(value, out _))
                    {
                        error = $"Field '{fieldName}' should be numeric but is {TypeChecking.DescribeType(value)}";
                        return false;
                    }
                    break;

                case "bool":
                case "boolean":
                    if (value is not bool)
                    {
                        error = $"Field '{fieldName}' should be bool but is {TypeChecking.DescribeType(value)}";
                        return false;
                    }
                    break;

                case "datetime":
                case "date":
                    if (value is not DateTime && value is not DateTimeOffset)
                    {
                        // Try to parse if it's a string
                        if (value is string strValue)
                        {
                            if (!DateTime.TryParse(strValue, out _))
                            {
                                error = $"Field '{fieldName}' should be datetime but string value cannot be parsed";
                                return false;
                            }
                        }
                        else
                        {
                            error = $"Field '{fieldName}' should be datetime but is {TypeChecking.DescribeType(value)}";
                            return false;
                        }
                    }
                    break;

                case var listType when listType.StartsWith("list<") && listType.EndsWith(">"):
                    // Extract inner type: list<string> -> string
                    var innerType = listType.Substring(5, listType.Length - 6).Trim();
                    
                    if (value is not System.Collections.IEnumerable enumerable)
                    {
                        error = $"Field '{fieldName}' should be a list but is {TypeChecking.DescribeType(value)}";
                        return false;
                    }

                    // Validate each element in the list
                    if (value is not string) // strings are enumerable but we don't want to treat them as lists
                    {
                        int index = 0;
                        foreach (var item in enumerable)
                        {
                            if (item != null && !ValidateFieldType($"{fieldName}[{index}]", item, innerType, out var itemError))
                            {
                                error = itemError;
                                return false;
                            }
                            index++;
                        }
                    }
                    else
                    {
                        error = $"Field '{fieldName}' should be a list but is string";
                        return false;
                    }
                    break;

                default:
                    // Unknown type - warn but don't fail
                    error = $"Field '{fieldName}' has unknown type '{expectedType}' - skipping type validation";
                    return true; // Don't fail on unknown types
            }

            return true;
        }

        /// <summary>
        /// Gets a summary of the schema being used for validation.
        /// </summary>
        public string GetSchemaSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Schema: {_schema.Name}");
            sb.AppendLine("Fields:");
            foreach (var field in _schema.Fields)
            {
                var required = field.Required ? " (Required)" : "";
                var signed = field.Signed ? " [Signed]" : "";
                var defaultVal = field.DefaultValue != null ? $" Default={field.DefaultValue}" : "";
                sb.AppendLine($"  - {field.Name}: {field.Type}{required}{signed}{defaultVal}");
            }
            return sb.ToString();
        }
    }
}