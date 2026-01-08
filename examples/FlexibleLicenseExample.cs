using SimpleLicense.Core;
using SimpleLicense.LicenseValidation;
using System;
using System.Collections.Generic;

namespace SimpleLicense.Examples
{
    /// <summary>
    /// Demonstrates creating, signing, and verifying flexible LicenseDocument objects
    /// with custom fields beyond the rigid License record structure.
    /// </summary>
    public class FlexibleLicenseExample
    {
        public static void Run()
        {
            Console.WriteLine();

            // 1. Generate RSA key pair for signing/verification (in-memory, not saved to files)
            ConsoleHelper.WriteHighlight("Step 1: Generating RSA key pair");
            ConsoleHelper.WriteSeparator();
            string privateKeyPem, publicKeyPem;
            using (var rsa = System.Security.Cryptography.RSA.Create(2048))
            {
                privateKeyPem = rsa.ExportRSAPrivateKeyPem();
                publicKeyPem = rsa.ExportRSAPublicKeyPem();
            }
            ConsoleHelper.WriteSuccess("✓ Keys generated successfully (2048-bit RSA, in-memory)");
            Console.WriteLine();

            // 2. Create a flexible license with custom fields
            ConsoleHelper.WriteHighlight("Step 2: Creating flexible license with custom fields");
            ConsoleHelper.WriteSeparator();
            var creator = new LicenseCreator("CUSTOM-LICENSE-2026")
            {
                NumberOfValidMonths = 12,
                LicenseInfo = "Premium Enterprise License",
                // Add custom fields specific to your application
                CustomFields = new Dictionary<string, object?>
                {
                    ["CompanyName"] = "Acme Corporation",
                    ["MaxUsers"] = 100,
                    ["Features"] = new List<string> { "Analytics", "API Access", "Priority Support" },
                    ["Tier"] = "Enterprise",
                    ["Region"] = "North America",
                    ["ContactEmail"] = "licensing@acme.com"
                }
            };

            var license = creator.CreateLicenseDocument();
            
            // You can also add fields after creation
            license["CustomMetadata"] = new Dictionary<string, object?>
            {
                ["CreatedBy"] = "License Server v2.0",
                ["IssuanceDate"] = DateTime.UtcNow.ToString("o")
            };
            
            ConsoleHelper.WriteSuccess("✓ License created with custom fields:");
            ConsoleHelper.WriteInfo($"  License ID: {license["LicenseId"]}");
            ConsoleHelper.WriteInfo($"  Company: {license["CompanyName"]}");
            ConsoleHelper.WriteInfo($"  Max Users: {license["MaxUsers"]}");
            ConsoleHelper.WriteInfo($"  Tier: {license["Tier"]}");
            ConsoleHelper.WriteInfo($"  Region: {license["Region"]}");
            ConsoleHelper.WriteInfo($"  Expires: {license["ExpiryUtc"]}");
            Console.WriteLine();

            // 3. Sign the license
            ConsoleHelper.WriteHighlight("Step 3: Signing the license");
            ConsoleHelper.WriteSeparator();
            var signer = new LicenseSigner(privateKeyPem);
            signer.SignLicenseDocument(license);
            var signature = license["Signature"]?.ToString();
            var shortSignature = signature?.Substring(0, Math.Min(40, signature.Length)) ?? "";
            ConsoleHelper.WriteSuccess($"✓ License signed successfully");
            ConsoleHelper.WriteInfo($"  Signature (first 40 chars): {shortSignature}...");
            Console.WriteLine();

            // 4. Serialize to JSON
            ConsoleHelper.WriteHighlight("Step 4: Serializing license to JSON");
            ConsoleHelper.WriteSeparator();
            string licenseJson = license.ToJson(validate: true);
            Console.WriteLine(licenseJson);
            Console.WriteLine();

            // 5. Verify the license
            ConsoleHelper.WriteHighlight("Step 5: Verifying license signature");
            ConsoleHelper.WriteSeparator();
            var verifier = new LicenseVerifier(publicKeyPem);
            bool isValid = verifier.VerifyLicenseDocument(license, out string? failureReason);
            
            if (isValid)
            {
                ConsoleHelper.WriteSuccess("✓ License signature is VALID");
            }
            else
            {
                ConsoleHelper.WriteError($"✗ License signature is INVALID: {failureReason}");
            }
            Console.WriteLine();

            // 6. Verify from JSON string
            ConsoleHelper.WriteHighlight("Step 6: Verifying license from JSON string");
            ConsoleHelper.WriteSeparator();
            isValid = verifier.VerifyLicenseDocumentJson(licenseJson, out failureReason);
            
            if (isValid)
            {
                ConsoleHelper.WriteSuccess("✓ License from JSON is VALID");
            }
            else
            {
                ConsoleHelper.WriteError($"✗ License from JSON is INVALID: {failureReason}");
            }
            Console.WriteLine();

            // 7. Demonstrate tampering detection
            ConsoleHelper.WriteHighlight("Step 7: Testing tampering detection");
            ConsoleHelper.WriteSeparator();
            var tamperedLicense = LicenseDocument.FromJson(licenseJson);
            ConsoleHelper.WriteInfo("  Modifying MaxUsers from 100 to 500...");
            tamperedLicense["MaxUsers"] = 500; // Tamper with the license
            
            isValid = verifier.VerifyLicenseDocument(tamperedLicense, out failureReason);
            if (isValid)
            {
                ConsoleHelper.WriteError("✗ WARNING: Tampered license verified (should not happen!)");
            }
            else
            {
                ConsoleHelper.WriteSuccess($"✓ Correctly detected tampering");
                ConsoleHelper.WriteInfo($"  Reason: {failureReason}");
            }
            Console.WriteLine();

            // 8. Access custom fields
            ConsoleHelper.WriteHighlight("Step 8: Accessing custom fields from verified license");
            ConsoleHelper.WriteSeparator();
            var verifiedLicense = LicenseDocument.FromJson(licenseJson);
            ConsoleHelper.WriteInfo($"  Company Name: {verifiedLicense["CompanyName"]}");
            ConsoleHelper.WriteInfo($"  Tier: {verifiedLicense["Tier"]}");
            ConsoleHelper.WriteInfo($"  Region: {verifiedLicense["Region"]}");
            
            if (verifiedLicense["Features"] is System.Collections.IEnumerable features)
            {
                Console.Write("  Features: ");
                foreach (var feature in features)
                {
                    Console.Write($"{feature}, ");
                }
                Console.WriteLine();
            }
            Console.WriteLine();

        }
    }
}
