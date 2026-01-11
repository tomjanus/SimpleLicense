using SimpleLicense.Core;
using SimpleLicense.Core.LicenseValidation;

namespace SimpleLicense.Examples
{
    /// <summary>
    /// Demonstrates using field processors to transform values during license creation.
    /// Shows built-in processors (HashFiles, GenerateGuid, CurrentTimestamp) and custom processors.
    /// </summary>
    public static class FieldProcessorExample
    {
        public static void Run()
        {
            ConsoleHelper.WriteInfo("This example demonstrates:");
            ConsoleHelper.WriteInfo("  • Using built-in field processors");
            ConsoleHelper.WriteInfo("  • Creating licenses with automatic field processing");
            ConsoleHelper.WriteInfo("  • File hash computation");
            ConsoleHelper.WriteInfo("  • Auto-generated GUIDs and timestamps");
            ConsoleHelper.WriteInfo("  • Registering custom processors");
            Console.WriteLine();

            // ═══════════════════════════════════════════════════════
            // Step 1: Create schema with processors
            // ═══════════════════════════════════════════════════════
            ConsoleHelper.WriteHighlight("Step 1: Define schema with field processors");
            ConsoleHelper.WriteSeparator();

            var schema = new LicenseSchema(
                "FileProtectionLicense",
                new List<FieldDescriptor>
                {
                    new("LicenseId", "string", Required: true, Processor: "GenerateGuid"),
                    new("CreatedUtc", "datetime", Required: true, Processor: "CurrentTimestamp"),
                    new("ExpiryUtc", "datetime", Required: true),
                    new("CustomerName", "string", Required: true, Processor: "ToUpper"),
                    new("ProtectedFiles", "list<string>", Required: false, Processor: "HashFiles"), // Not required to skip type validation
                    new("Signature", "string", Required: false)
                }
            );

            ConsoleHelper.WriteSuccess("✓ Schema created with processors:");
            foreach (var field in schema.Fields)
            {
                var processor = field.Processor ?? "(none)";
                Console.WriteLine($"    {field.Name} ({field.Type}) - Processor: {processor}");
            }
            Console.WriteLine();

            // Save schema for reference
            LicenseSchema.ToFile(schema, "./outputs/file_protection_schema.yaml");
            ConsoleHelper.WriteSuccess("✓ Schema saved to: ./outputs/file_protection_schema.yaml");
            Console.WriteLine();

            // ═══════════════════════════════════════════════════════
            // Step 2: Create license with raw input values
            // ═══════════════════════════════════════════════════════
            ConsoleHelper.WriteHighlight("Step 2: Create license with raw input (before processing)");
            ConsoleHelper.WriteSeparator();

            // Ensure test files exist
            Directory.CreateDirectory("./data");
            File.WriteAllText("./data/file1.txt", "This is test file 1");
            File.WriteAllText("./data/file2.txt", "This is test file 2");

            var rawInput = new Dictionary<string, object?>
            {
                ["ExpiryUtc"] = "2027-12-31T23:59:59Z",
                ["CustomerName"] = "acme corporation", // Will be converted to uppercase
                ["ProtectedFiles"] = new List<string> { "./data/file1.txt", "./data/file2.txt" }
                // LicenseId and CreatedUtc will be auto-generated
            };

            ConsoleHelper.WriteInfo("Raw input values:");
            foreach (var kvp in rawInput)
            {
                var displayValue = kvp.Value is List<string> list 
                    ? $"[{string.Join(", ", list)}]" 
                    : kvp.Value?.ToString() ?? "null";
                Console.WriteLine($"    {kvp.Key} = {displayValue}");
            }
            Console.WriteLine();

            // ═══════════════════════════════════════════════════════
            // Step 3: Process fields and create license
            // ═══════════════════════════════════════════════════════
            ConsoleHelper.WriteHighlight("Step 3: Process fields and create license");
            ConsoleHelper.WriteSeparator();

            var creator = new LicenseCreator();
            creator.OnInfo += msg => ConsoleHelper.WriteInfo($"  [Creator] {msg}");

            var license = creator.CreateLicense(
                schema,
                rawInput,
                workingDirectory: Directory.GetCurrentDirectory()
            );

            ConsoleHelper.WriteSuccess("✓ License created with processed values:");
            Console.WriteLine();

            // ═══════════════════════════════════════════════════════
            // Step 4: Display processed results
            // ═══════════════════════════════════════════════════════
            ConsoleHelper.WriteHighlight("Step 4: Examine processed field values");
            ConsoleHelper.WriteSeparator();

            ConsoleHelper.WriteInfo("Processed field values:");
            Console.WriteLine();

            // LicenseId (auto-generated GUID)
            Console.WriteLine($"  LicenseId (Generated GUID):");
            ConsoleHelper.WriteSuccess($"    {license["LicenseId"]}");
            Console.WriteLine();

            // CreatedUtc (auto-generated timestamp)
            Console.WriteLine($"  CreatedUtc (Auto-generated timestamp):");
            ConsoleHelper.WriteSuccess($"    {license["CreatedUtc"]}");
            Console.WriteLine();

            // CustomerName (converted to uppercase)
            Console.WriteLine($"  CustomerName (Converted to uppercase):");
            Console.WriteLine($"    Input:  'acme corporation'");
            ConsoleHelper.WriteSuccess($"    Output: '{license["CustomerName"]}'");
            Console.WriteLine();

            // ProtectedFiles (hashed)
            Console.WriteLine($"  ProtectedFiles (File hashes computed):");
            if (license["ProtectedFiles"] is Dictionary<string, string> fileHashes)
            {
                foreach (var kvp in fileHashes)
                {
                    ConsoleHelper.WriteSuccess($"    {kvp.Key}:");
                    Console.WriteLine($"      SHA256: {kvp.Value}");
                }
            }
            Console.WriteLine();

            // ═══════════════════════════════════════════════════════
            // Step 5: Serialize and validate
            // ═══════════════════════════════════════════════════════
            ConsoleHelper.WriteHighlight("Step 5: Serialize license");
            ConsoleHelper.WriteSeparator();

            var json = license.ToJson(validate: true);
            File.WriteAllText("./outputs/processed_license.json", json);
            ConsoleHelper.WriteSuccess("✓ License serialized to: ./outputs/processed_license.json");

            ConsoleHelper.WriteInfo("");
            ConsoleHelper.WriteInfo("Note: Schema validation skipped for this example because the HashFiles");
            ConsoleHelper.WriteInfo("processor transforms list<string> input to Dictionary<string,string> output,");
            ConsoleHelper.WriteInfo("which doesn't match the schema's type definition. This is expected behavior");
            ConsoleHelper.WriteInfo("when processors transform data types.");
            Console.WriteLine();

            // ═══════════════════════════════════════════════════════
            // Step 6: Custom processor registration
            // ═══════════════════════════════════════════════════════
            ConsoleHelper.WriteHighlight("Step 6: Register and use custom processor");
            ConsoleHelper.WriteSeparator();

            // Register a custom processor that adds a prefix
            FieldProcessors.Register("AddPrefix", (value, context) =>
            {
                var prefix = context.Parameters.TryGetValue("prefix", out var p) 
                    ? p.ToString() 
                    : "PREFIX-";
                return $"{prefix}{value}";
            });

            ConsoleHelper.WriteSuccess("✓ Registered custom processor 'AddPrefix'");

            // Create a schema using the custom processor
            var customSchema = new LicenseSchema(
                "CustomProcessorDemo",
                new List<FieldDescriptor>
                {
                    new("LicenseId", "string", Required: true, Processor: "AddPrefix"),
                    new("ExpiryUtc", "datetime", Required: true),
                    new("Signature", "string", Required: false)
                }
            );

            var customInput = new Dictionary<string, object?>
            {
                ["LicenseId"] = "12345",
                ["ExpiryUtc"] = "2027-12-31T23:59:59Z"
            };

            var customParams = new Dictionary<string, object>
            {
                ["prefix"] = "CUSTOM-LIC-"
            };

            var customLicense = creator.CreateLicense(
                customSchema,
                customInput,
                processorParameters: customParams
            );

            Console.WriteLine($"  Input LicenseId:  '12345'");
            ConsoleHelper.WriteSuccess($"  Output LicenseId: '{customLicense["LicenseId"]}'");
            Console.WriteLine();

            // ═══════════════════════════════════════════════════════
            // Step 7: List available processors
            // ═══════════════════════════════════════════════════════
            ConsoleHelper.WriteHighlight("Step 7: Available field processors");
            ConsoleHelper.WriteSeparator();

            ConsoleHelper.WriteInfo("Built-in and registered processors:");
            foreach (var processor in FieldProcessors.All.Keys.OrderBy(k => k))
            {
                Console.WriteLine($"    • {processor}");
            }
            Console.WriteLine();

            ConsoleHelper.WriteSuccess("✓ Field processor example completed successfully!");
        }
    }
}
