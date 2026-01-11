using SimpleLicense.Core;
using SimpleLicense.Core.LicenseValidation;
using System.Security.Cryptography;

namespace SimpleLicense.Examples
{
    /// <summary>
    /// Demonstrates comprehensive license signing and verification workflow.
    /// Shows how to create, sign, verify licenses and detect tampering.
    /// </summary>
    public static class SigningAndVerificationExample
    {
        public static void Run()
        {
            ConsoleHelper.WriteInfo("This example demonstrates:");
            ConsoleHelper.WriteInfo("  • Generating RSA key pairs for signing/verification");
            ConsoleHelper.WriteInfo("  • Creating and signing licenses");
            ConsoleHelper.WriteInfo("  • Verifying signed licenses");
            ConsoleHelper.WriteInfo("  • Detecting tampered licenses");
            ConsoleHelper.WriteInfo("  • Canonical serialization for signature consistency");
            Console.WriteLine();

            // ═══════════════════════════════════════════════════════
            // Step 1: Generate RSA key pair
            // ═══════════════════════════════════════════════════════
            ConsoleHelper.WriteHighlight("Step 1: Generate RSA key pair");
            ConsoleHelper.WriteSeparator();

            string privateKeyPem, publicKeyPem;
            using (var rsa = RSA.Create(2048))
            {
                privateKeyPem = rsa.ExportRSAPrivateKeyPem();
                publicKeyPem = rsa.ExportRSAPublicKeyPem();
            }

            ConsoleHelper.WriteSuccess("✓ Generated 2048-bit RSA key pair");
            ConsoleHelper.WriteInfo($"  Private key: {privateKeyPem.Length} chars (keep secret!)");
            ConsoleHelper.WriteInfo($"  Public key: {publicKeyPem.Length} chars (distribute to verifiers)");
            Console.WriteLine();

            // ═══════════════════════════════════════════════════════
            // Step 2: Create a license
            // ═══════════════════════════════════════════════════════
            ConsoleHelper.WriteHighlight("Step 2: Create a license");
            ConsoleHelper.WriteSeparator();

            var schema = new LicenseSchema(
                "StandardLicense",
                new List<FieldDescriptor>
                {
                    new("LicenseId", "string", Required: true, Processor: "GenerateGuid"),
                    new("CustomerName", "string", Required: true),
                    new("ExpiryUtc", "datetime", Required: true),
                    new("MaxUsers", "int", Required: true),
                    new("Features", "list<string>", Required: false),
                    new("Signature", "string", Required: false)
                }
            );

            var creator = new LicenseCreator();
            var license = creator.CreateLicense(schema, new Dictionary<string, object?>
            {
                ["CustomerName"] = "Acme Corporation",
                ["ExpiryUtc"] = DateTime.UtcNow.AddYears(1),
                ["MaxUsers"] = 50,
                ["Features"] = new List<string> { "Analytics", "API", "Support" }
            });

            ConsoleHelper.WriteSuccess("✓ License created:");
            ConsoleHelper.WriteInfo($"  License ID: {license["LicenseId"]}");
            ConsoleHelper.WriteInfo($"  Customer: {license["CustomerName"]}");
            ConsoleHelper.WriteInfo($"  Max Users: {license["MaxUsers"]}");
            ConsoleHelper.WriteInfo($"  Expires: {license["ExpiryUtc"]}");
            ConsoleHelper.WriteInfo($"  Signature: null (not yet signed)");
            Console.WriteLine();

            // ═══════════════════════════════════════════════════════
            // Step 3: Sign the license
            // ═══════════════════════════════════════════════════════
            ConsoleHelper.WriteHighlight("Step 3: Sign the license");
            ConsoleHelper.WriteSeparator();

            var signer = new LicenseSigner(privateKeyPem);
            signer.SignLicenseDocument(license);

            var signature = license["Signature"]?.ToString() ?? "";
            ConsoleHelper.WriteSuccess("✓ License signed successfully");
            ConsoleHelper.WriteInfo($"  Signature length: {signature.Length} chars (base64)");
            ConsoleHelper.WriteInfo($"  Signature preview: {signature.Substring(0, Math.Min(60, signature.Length))}...");
            Console.WriteLine();

            ConsoleHelper.WriteInfo("How signing works:");
            ConsoleHelper.WriteInfo("  1. License fields are serialized in canonical form (sorted, no signature)");
            ConsoleHelper.WriteInfo("  2. SHA256 hash is computed from canonical bytes");
            ConsoleHelper.WriteInfo("  3. Hash is signed with RSA private key");
            ConsoleHelper.WriteInfo("  4. Signature is base64-encoded and stored in Signature field");
            Console.WriteLine();

            // ═══════════════════════════════════════════════════════
            // Step 4: Serialize signed license
            // ═══════════════════════════════════════════════════════
            ConsoleHelper.WriteHighlight("Step 4: Serialize signed license");
            ConsoleHelper.WriteSeparator();

            var licenseJson = license.ToJson();
            File.WriteAllText("./outputs/signed_license.json", licenseJson);
            
            ConsoleHelper.WriteSuccess("✓ Signed license saved to: ./outputs/signed_license.json");
            ConsoleHelper.WriteInfo($"  JSON size: {licenseJson.Length} bytes");
            Console.WriteLine();

            // ═══════════════════════════════════════════════════════
            // Step 5: Verify the signed license
            // ═══════════════════════════════════════════════════════
            ConsoleHelper.WriteHighlight("Step 5: Verify the signed license");
            ConsoleHelper.WriteSeparator();

            var verifier = new LicenseVerifier(publicKeyPem);
            var isValid = verifier.VerifyLicenseDocument(license, out var failureReason);

            if (isValid)
            {
                ConsoleHelper.WriteSuccess("✓ License signature is VALID");
                ConsoleHelper.WriteInfo("  The license has not been tampered with");
                ConsoleHelper.WriteInfo("  All fields are authentic");
            }
            else
            {
                ConsoleHelper.WriteError($"✗ License signature is INVALID: {failureReason}");
            }
            Console.WriteLine();

            ConsoleHelper.WriteInfo("How verification works:");
            ConsoleHelper.WriteInfo("  1. Extract signature from license");
            ConsoleHelper.WriteInfo("  2. Temporarily remove signature field");
            ConsoleHelper.WriteInfo("  3. Serialize license in canonical form");
            ConsoleHelper.WriteInfo("  4. Compute SHA256 hash from canonical bytes");
            ConsoleHelper.WriteInfo("  5. Verify signature using RSA public key");
            Console.WriteLine();

            // ═══════════════════════════════════════════════════════
            // Step 6: Demonstrate tamper detection
            // ═══════════════════════════════════════════════════════
            ConsoleHelper.WriteHighlight("Step 6: Demonstrate tamper detection");
            ConsoleHelper.WriteSeparator();

            // Create a copy and tamper with it
            var tamperedLicense = License.FromJson(license.ToJson());
            ConsoleHelper.WriteInfo("Original MaxUsers: 50");
            
            // Attacker tries to modify MaxUsers
            tamperedLicense["MaxUsers"] = 999;
            ConsoleHelper.WriteWarning("⚠ Tampered MaxUsers: 999 (unauthorized change)");
            Console.WriteLine();

            // Verify tampered license
            var isTamperedValid = verifier.VerifyLicenseDocument(tamperedLicense, out var tamperedReason);

            if (!isTamperedValid)
            {
                ConsoleHelper.WriteSuccess("✓ Tamper detected successfully!");
                ConsoleHelper.WriteError($"  Verification failed: {tamperedReason}");
                ConsoleHelper.WriteInfo("  The signature does not match the modified content");
                ConsoleHelper.WriteInfo("  This license cannot be trusted");
            }
            else
            {
                ConsoleHelper.WriteError("✗ ERROR: Tampered license incorrectly verified!");
            }
            Console.WriteLine();

            // ═══════════════════════════════════════════════════════
            // Step 7: Load and verify from JSON
            // ═══════════════════════════════════════════════════════
            ConsoleHelper.WriteHighlight("Step 7: Load and verify from JSON file");
            ConsoleHelper.WriteSeparator();

            var loadedJson = File.ReadAllText("./outputs/signed_license.json");
            var loadedValid = verifier.VerifyLicenseDocumentJson(loadedJson, out var loadedReason);

            if (loadedValid)
            {
                ConsoleHelper.WriteSuccess("✓ Loaded license signature is VALID");
                ConsoleHelper.WriteInfo("  License loaded from file is authentic");
            }
            else
            {
                ConsoleHelper.WriteError($"✗ Loaded license signature is INVALID: {loadedReason}");
            }
            Console.WriteLine();

            // ═══════════════════════════════════════════════════════
            // Step 8: Test with wrong public key
            // ═══════════════════════════════════════════════════════
            ConsoleHelper.WriteHighlight("Step 8: Test with wrong public key");
            ConsoleHelper.WriteSeparator();

            string wrongPublicKey;
            using (var rsa = RSA.Create(2048))
            {
                wrongPublicKey = rsa.ExportRSAPublicKeyPem();
            }

            var wrongVerifier = new LicenseVerifier(wrongPublicKey);
            var wrongKeyValid = wrongVerifier.VerifyLicenseDocument(license, out var wrongKeyReason);

            if (!wrongKeyValid)
            {
                ConsoleHelper.WriteSuccess("✓ Wrong key detected successfully!");
                ConsoleHelper.WriteError($"  Verification failed: {wrongKeyReason}");
                ConsoleHelper.WriteInfo("  License signed with different private key");
            }
            else
            {
                ConsoleHelper.WriteError("✗ ERROR: Wrong key incorrectly verified!");
            }
            Console.WriteLine();

            // ═══════════════════════════════════════════════════════
            // Step 9: Canonical serialization consistency
            // ═══════════════════════════════════════════════════════
            ConsoleHelper.WriteHighlight("Step 9: Canonical serialization consistency");
            ConsoleHelper.WriteSeparator();

            var serializer = new CanonicalLicenseSerializer();
            
            // Remove signature for canonical comparison
            license["Signature"] = null;
            var canonical1 = serializer.SerializeLicenseDocument(license);
            
            // Deserialize and re-serialize - should produce identical bytes
            var roundtrip = License.FromJson(license.ToJson());
            roundtrip["Signature"] = null;
            var canonical2 = serializer.SerializeLicenseDocument(roundtrip);

            bool identical = canonical1.SequenceEqual(canonical2);
            if (identical)
            {
                ConsoleHelper.WriteSuccess("✓ Canonical serialization is consistent");
                ConsoleHelper.WriteInfo("  Same license produces identical bytes");
                ConsoleHelper.WriteInfo($"  Canonical size: {canonical1.Length} bytes");
                ConsoleHelper.WriteInfo("  This ensures signatures remain valid after serialization");
            }
            else
            {
                ConsoleHelper.WriteError("✗ Canonical serialization inconsistent!");
            }
            Console.WriteLine();

            // ═══════════════════════════════════════════════════════
            // Step 10: Summary
            // ═══════════════════════════════════════════════════════
            ConsoleHelper.WriteHighlight("Step 10: Security summary");
            ConsoleHelper.WriteSeparator();

            ConsoleHelper.WriteInfo("Key security features:");
            ConsoleHelper.WriteSuccess("  ✓ Cryptographic signatures prevent tampering");
            ConsoleHelper.WriteSuccess("  ✓ Any field modification invalidates signature");
            ConsoleHelper.WriteSuccess("  ✓ Only holder of private key can sign licenses");
            ConsoleHelper.WriteSuccess("  ✓ Public key can be freely distributed for verification");
            ConsoleHelper.WriteSuccess("  ✓ Canonical serialization ensures consistency");
            Console.WriteLine();

            ConsoleHelper.WriteInfo("Best practices:");
            ConsoleHelper.WriteInfo("  • Keep private keys secure (never distribute)");
            ConsoleHelper.WriteInfo("  • Use strong RSA keys (2048+ bits)");
            ConsoleHelper.WriteInfo("  • Verify signatures before trusting license data");
            ConsoleHelper.WriteInfo("  • Store public keys securely in application");
            ConsoleHelper.WriteInfo("  • Consider key rotation policies");
            Console.WriteLine();

            ConsoleHelper.WriteSuccess("✓ Signing and verification example completed successfully!");
        }
    }
}
