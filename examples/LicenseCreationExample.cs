using SimpleLicense.Core;
using SimpleLicense.Core.LicenseValidation;

namespace SimpleLicense.Examples
{
    /// <summary>
    /// Demonstrates creating a license, validating fields, and validating against schemas.
    /// Shows both JSON and YAML schema validation.
    /// </summary>
    public static class LicenseCreationExample
    {
        public static void Run()
        {
            ConsoleHelper.WriteInfo("This example demonstrates:");
            ConsoleHelper.WriteInfo("  • Creating a license from JSON");
            ConsoleHelper.WriteInfo("  • Field-level validation");
            ConsoleHelper.WriteInfo("  • Schema-level validation (JSON format)");
            ConsoleHelper.WriteInfo("  • Schema-level validation (YAML format)");
            Console.WriteLine();

            // ═══════════════════════════════════════════════════════
            // Step 1: Create a license from JSON
            // ═══════════════════════════════════════════════════════
            ConsoleHelper.WriteHighlight("Step 1: Creating license from ./data/example_user_license.lic");
            ConsoleHelper.WriteSeparator();

            var license = new License();
            
            // Set mandatory fields (LicenseId was null in the example, so we'll set it)
            license["LicenseId"] = "USR-LIC-2026-001";
            license["ExpiryUtc"] = "2027-01-01T00:00:00Z";
            license["Signature"] = "EXAMPLE-SIGNATURE-ABC123"; // Required mandatory field
            
            // Set custom fields from example
            license["Files"] = new List<string> { ".data/file1.txt", ".data/file2.txt" };
            license["Notes"] = "Example license adhering to 'example_license_template.json'";
            
            ConsoleHelper.WriteSuccess("✓ License created with fields:");
            foreach (var field in license.Fields)
            {
                var value = field.Value;
                var displayValue = value is List<string> list 
                    ? $"[{string.Join(", ", list)}]" 
                    : value?.ToString() ?? "null";
                Console.WriteLine($"    {field.Key} = {displayValue}");
            }
            Console.WriteLine();

            // ═══════════════════════════════════════════════════════
            // Step 2: Field-level validation
            // ═══════════════════════════════════════════════════════
            ConsoleHelper.WriteHighlight("Step 2: Field-level validation (mandatory fields)");
            ConsoleHelper.WriteSeparator();

            try
            {
                license.EnsureMandatoryPresent();
                ConsoleHelper.WriteSuccess("✓ All mandatory fields are valid:");
                Console.WriteLine($"    LicenseId: {license["LicenseId"]}");
                Console.WriteLine($"    ExpiryUtc: {license["ExpiryUtc"]}");
                Console.WriteLine($"    Signature: {license["Signature"]}");
            }
            catch (LicenseValidationException ex)
            {
                ConsoleHelper.WriteError("✗ Field validation failed:");
                foreach (var issue in ex.Issues)
                {
                    ConsoleHelper.WriteError($"    - {issue}");
                }
                return; // Stop if validation fails
            }
            Console.WriteLine();

            // ═══════════════════════════════════════════════════════
            // Step 3: Schema-level validation (JSON format)
            // ═══════════════════════════════════════════════════════
            ConsoleHelper.WriteHighlight("Step 3: Schema-level validation (JSON format)");
            ConsoleHelper.WriteSeparator();

            // Create a schema that matches our license structure
            var schemaJson = new LicenseSchema(
                "UserLicenseSchema",
                new List<FieldDescriptor>
                {
                    new("LicenseId", "string", Required: true),
                    new("ExpiryUtc", "datetime", Required: true),
                    new("Signature", "string", Required: true),
                    new("Files", "list<string>", Required: false),
                    new("Notes", "string", Required: false)
                }
            );

            // Save schema to JSON file
            LicenseSchema.ToFile(schemaJson, "./outputs/user_license_schema.json");
            ConsoleHelper.WriteSuccess("✓ Schema saved to: ./outputs/user_license_schema.json");

            // Load schema from JSON and validate
            var loadedSchemaJson = LicenseSchema.FromFile("./outputs/user_license_schema.json");
            ConsoleHelper.WriteSuccess("✓ Schema loaded from JSON file");

            var validatorJson = new LicenseValidator(loadedSchemaJson);
            if (validatorJson.Validate(license, out var errorsJson))
            {
                ConsoleHelper.WriteSuccess("✓ License is valid according to JSON schema");
                ConsoleHelper.WriteInfo($"    Schema: {loadedSchemaJson.Name}");
                ConsoleHelper.WriteInfo($"    Fields defined: {loadedSchemaJson.Fields.Count}");
            }
            else
            {
                ConsoleHelper.WriteError("✗ Schema validation (JSON) failed:");
                foreach (var error in errorsJson)
                {
                    ConsoleHelper.WriteError($"    - {error}");
                }
                return; // Stop if validation fails
            }
            Console.WriteLine();

            // ═══════════════════════════════════════════════════════
            // Step 4: Schema-level validation (YAML format)
            // ═══════════════════════════════════════════════════════
            ConsoleHelper.WriteHighlight("Step 4: Schema-level validation (YAML format)");
            ConsoleHelper.WriteSeparator();

            // Save the same schema to YAML file
            LicenseSchema.ToFile(schemaJson, "./outputs/user_license_schema.yaml");
            ConsoleHelper.WriteSuccess("✓ Schema saved to: ./outputs/user_license_schema.yaml");

            // Display YAML content
            var yamlContent = File.ReadAllText("./outputs/user_license_schema.yaml");
            ConsoleHelper.WriteInfo("  YAML content:");
            foreach (var line in yamlContent.Split('\n').Take(10))
            {
                if (!string.IsNullOrWhiteSpace(line))
                    ConsoleHelper.WriteInfo($"    {line}");
            }

            // Load schema from YAML and validate
            var loadedSchemaYaml = LicenseSchema.FromFile("./outputs/user_license_schema.yaml");
            ConsoleHelper.WriteSuccess("✓ Schema loaded from YAML file");

            var validatorYaml = new LicenseValidator(loadedSchemaYaml);
            if (validatorYaml.Validate(license, out var errorsYaml))
            {
                ConsoleHelper.WriteSuccess("✓ License is valid according to YAML schema");
                ConsoleHelper.WriteInfo($"    Schema: {loadedSchemaYaml.Name}");
                ConsoleHelper.WriteInfo($"    Fields defined: {loadedSchemaYaml.Fields.Count}");
            }
            else
            {
                ConsoleHelper.WriteError("✗ Schema validation (YAML) failed:");
                foreach (var error in errorsYaml)
                {
                    ConsoleHelper.WriteError($"    - {error}");
                }
                return; // Stop if validation fails
            }
            Console.WriteLine();

            // ═══════════════════════════════════════════════════════
            // Step 5: Demonstrate schema validation details
            // ═══════════════════════════════════════════════════════
            ConsoleHelper.WriteHighlight("Step 5: Schema validation details");
            ConsoleHelper.WriteSeparator();

            ConsoleHelper.WriteInfo("Schema summary:");
            Console.WriteLine(validatorJson.GetSchemaSummary());
            Console.WriteLine();

            // ═══════════════════════════════════════════════════════
            // Step 6: Save the validated license
            // ═══════════════════════════════════════════════════════
            ConsoleHelper.WriteHighlight("Step 6: Saving validated license");
            ConsoleHelper.WriteSeparator();

            var licenseJson = license.ToJson();
            File.WriteAllText("./outputs/validated_user_license.json", licenseJson);
            ConsoleHelper.WriteSuccess("✓ License saved to: ./outputs/validated_user_license.json");
            Console.WriteLine();
            ConsoleHelper.WriteInfo("  License JSON:");
            foreach (var line in licenseJson.Split('\n').Take(10))
            {
                ConsoleHelper.WriteInfo($"    {line}");
            }
            ConsoleHelper.WriteInfo("    ...");
        }
    }
}
