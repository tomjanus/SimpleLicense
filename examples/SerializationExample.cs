using SimpleLicense.Core;
using SimpleLicense.Core.LicenseValidation;
using System;
using System.Collections.Generic;

namespace SimpleLicense.Examples
{
    /// <summary>
    /// Demonstrates serialization and deserialization of LicenseDocument objects
    /// in different JSON formats (canonical vs pretty-printed).
    /// </summary>
    public class SerializationExample
    {
        public static void Run()
        {
            ConsoleHelper.WriteHeader("═══════════════════════════════════════════════════════");
            ConsoleHelper.WriteHeader("    License Serialization Example");
            ConsoleHelper.WriteHeader("═══════════════════════════════════════════════════════");
            Console.WriteLine();

            // ========================================================================
            // Step 1: Create a sample license with various field types
            // ========================================================================
            ConsoleHelper.WriteHighlight("Step 1: Creating a sample license with diverse fields");
            ConsoleHelper.WriteSeparator();
            
            var license = new License(ensureMandatoryKeys: true);
            license["LicenseId"] = "DEMO-SERIAL-2026-001";
            license["ExpiryUtc"] = DateTime.UtcNow.AddYears(1);
            license["Signature"] = null; // Will be set later
            
            // Add various types of custom fields
            license["CompanyName"] = "Acme Corporation";
            license["Tier"] = "Enterprise";
            license["MaxUsers"] = 500;
            license["IsActive"] = true;
            license["Features"] = new List<string> 
            { 
                "Advanced Analytics", 
                "API Access", 
                "24/7 Support",
                "Custom Integrations"
            };
            license["Metadata"] = new Dictionary<string, object?>
            {
                ["Version"] = "2.0",
                ["Environment"] = "Production",
                ["CreatedBy"] = "License Server",
                ["IssuanceDate"] = DateTime.UtcNow.ToString("o")
            };
            
            ConsoleHelper.WriteSuccess("✓ License document created");
            ConsoleHelper.WriteInfo($"  License ID: {license["LicenseId"]}");
            ConsoleHelper.WriteInfo($"  Company: {license["CompanyName"]}");
            ConsoleHelper.WriteInfo($"  Max Users: {license["MaxUsers"]}");
            ConsoleHelper.WriteInfo($"  Tier: {license["Tier"]}");
            Console.WriteLine();

            // ========================================================================
            // Step 2: Canonical (Compact) Serialization
            // ========================================================================
            ConsoleHelper.WriteHighlight("Step 2: Canonical serialization (compact, for signing)");
            ConsoleHelper.WriteSeparator();
            
            var canonicalSerializer = new LicenseSerializer(JsonProfiles.Canonical);
            var canonicalJson = canonicalSerializer.SerializeLicenseDocument(license);
            
            ConsoleHelper.WriteInfo("Canonical JSON (compact, deterministic):");
            Console.WriteLine(canonicalJson);
            Console.WriteLine();
            ConsoleHelper.WriteInfo($"Size: {canonicalJson.Length} characters");
            Console.WriteLine();

            // ========================================================================
            // Step 3: Pretty-Printed Serialization
            // ========================================================================
            ConsoleHelper.WriteHighlight("Step 3: Pretty-printed serialization (human-readable)");
            ConsoleHelper.WriteSeparator();
            
            var prettySerializer = new LicenseSerializer(JsonProfiles.Pretty);
            var prettyJson = prettySerializer.SerializeLicenseDocument(license);
            
            ConsoleHelper.WriteInfo("Pretty-printed JSON (human-readable):");
            Console.WriteLine(prettyJson);
            Console.WriteLine();
            ConsoleHelper.WriteInfo($"Size: {prettyJson.Length} characters");
            Console.WriteLine();

            // ========================================================================
            // Step 4: Deserialization and Round-Trip Verification
            // ========================================================================
            ConsoleHelper.WriteHighlight("Step 4: Deserialization and round-trip verification");
            ConsoleHelper.WriteSeparator();
            
            // Deserialize from canonical JSON
            var recoveredFromCanonical = canonicalSerializer.DeserializeLicenseDocument(canonicalJson);
            ConsoleHelper.WriteSuccess("✓ Successfully deserialized from canonical JSON");
            ConsoleHelper.WriteInfo($"  License ID: {recoveredFromCanonical["LicenseId"]}");
            ConsoleHelper.WriteInfo($"  Company: {recoveredFromCanonical["CompanyName"]}");
            
            // Deserialize from pretty JSON
            var recoveredFromPretty = prettySerializer.DeserializeLicenseDocument(prettyJson);
            ConsoleHelper.WriteSuccess("✓ Successfully deserialized from pretty JSON");
            
            // Verify both produce the same data
            var match = recoveredFromCanonical["LicenseId"]?.Equals(recoveredFromPretty["LicenseId"]) ?? false;
            if (match)
            {
                ConsoleHelper.WriteSuccess("✓ Both formats produce identical data after deserialization");
            }
            Console.WriteLine();

            // ========================================================================
            // Step 5: Byte Array Serialization
            // ========================================================================
            ConsoleHelper.WriteHighlight("Step 5: Byte array serialization (for storage/transmission)");
            ConsoleHelper.WriteSeparator();
            
            var bytes = canonicalSerializer.SerializeLicenseDocumentToBytes(license);
            ConsoleHelper.WriteSuccess($"✓ Serialized to byte array: {bytes.Length} bytes");
            
            // Deserialize from bytes
            var recoveredFromBytes = canonicalSerializer.DeserializeLicenseDocumentFromBytes(bytes);
            ConsoleHelper.WriteSuccess("✓ Successfully deserialized from byte array");
            ConsoleHelper.WriteInfo($"  License ID: {recoveredFromBytes["LicenseId"]}");
            Console.WriteLine();

            // ========================================================================
            // Step 6: Working with Nested Data Structures
            // ========================================================================
            ConsoleHelper.WriteHighlight("Step 6: Accessing nested data after deserialization");
            ConsoleHelper.WriteSeparator();
            
            // Access the Features list
            if (recoveredFromCanonical["Features"] is System.Collections.IEnumerable features)
            {
                ConsoleHelper.WriteInfo("Features:");
                foreach (var feature in features)
                {
                    Console.WriteLine($"    • {feature}");
                }
            }
            
            // Access the Metadata dictionary
            if (recoveredFromCanonical["Metadata"] is Dictionary<string, object> metadata)
            {
                ConsoleHelper.WriteInfo("\nMetadata:");
                foreach (var kvp in metadata)
                {
                    Console.WriteLine($"    {kvp.Key}: {kvp.Value}");
                }
            }
            Console.WriteLine();

            // ========================================================================
            // Step 7: Validation During Serialization
            // ========================================================================
            ConsoleHelper.WriteHighlight("Step 7: Validation during serialization");
            ConsoleHelper.WriteSeparator();
            
            // Create a valid license
            var validLicense = new License(ensureMandatoryKeys: true);
            validLicense["LicenseId"] = "VALID-001";
            validLicense["ExpiryUtc"] = DateTime.UtcNow.AddMonths(6);
            validLicense["Signature"] = "valid-sig-123";
            
            try
            {
                var validJson = canonicalSerializer.SerializeLicenseDocument(validLicense, validate: true);
                ConsoleHelper.WriteSuccess("✓ Valid license serialized successfully with validation");
            }
            catch (LicenseValidationException ex)
            {
                ConsoleHelper.WriteError($"✗ Validation failed: {ex.Message}");
            }
            
            // Create an invalid license (missing mandatory fields)
            var invalidLicense = new License(ensureMandatoryKeys: false);
            invalidLicense["CustomField"] = "Some value";
            
            try
            {
                var invalidJson = canonicalSerializer.SerializeLicenseDocument(invalidLicense, validate: true);
                ConsoleHelper.WriteError("✗ Invalid license should not have been serialized!");
            }
            catch (LicenseValidationException)
            {
                ConsoleHelper.WriteSuccess("✓ Correctly rejected invalid license during validation");
                ConsoleHelper.WriteInfo($"  Reason: Missing mandatory fields");
            }
            Console.WriteLine();

        }
    }
}
