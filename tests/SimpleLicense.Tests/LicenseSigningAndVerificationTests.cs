using SimpleLicense.Core;
using SimpleLicense.Core.LicenseValidation;
using System.Security.Cryptography;
using Xunit;

namespace SimpleLicense.Tests
{
    public class LicenseSigningAndVerificationTests
    {
        private readonly string _privateKeyPem;
        private readonly string _publicKeyPem;
        private readonly LicenseSchema _testSchema;

        public LicenseSigningAndVerificationTests()
        {
            // Generate test RSA key pair
            using var rsa = RSA.Create(2048);
            _privateKeyPem = rsa.ExportRSAPrivateKeyPem();
            _publicKeyPem = rsa.ExportRSAPublicKeyPem();

            // Create test schema
            _testSchema = new LicenseSchema(
                "TestLicense",
                new List<FieldDescriptor>
                {
                    new("LicenseId", "string", Required: true),
                    new("CustomerName", "string", Required: true),
                    new("ExpiryUtc", "datetime", Required: true),
                    new("MaxUsers", "int", Required: true),
                    new("Signature", "string", Required: false)
                }
            );
        }

        private License CreateTestLicense()
        {
            var creator = new LicenseCreator();
            return creator.CreateLicense(_testSchema, new Dictionary<string, object?>
            {
                ["LicenseId"] = "TEST-123",
                ["CustomerName"] = "Test Corp",
                ["ExpiryUtc"] = new DateTime(2027, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                ["MaxUsers"] = 10
            });
        }

        // ═══════════════════════════════════════════════════════
        // SIGNING TESTS
        // ═══════════════════════════════════════════════════════

        [Fact]
        public void SignLicenseDocument_AddsSignatureToLicense()
        {
            // Arrange
            var license = CreateTestLicense();
            var signer = new LicenseSigner(_privateKeyPem);
            Assert.Null(license["Signature"]);

            // Act
            var result = signer.SignLicenseDocument(license);

            // Assert
            Assert.NotNull(result["Signature"]);
            Assert.IsType<string>(result["Signature"]);
            var signature = result["Signature"]!.ToString()!;
            Assert.NotEmpty(signature);
            Assert.True(signature.Length > 100); // RSA signature should be substantial
        }

        [Fact]
        public void SignLicenseDocument_ReturnsSameInstance()
        {
            // Arrange
            var license = CreateTestLicense();
            var signer = new LicenseSigner(_privateKeyPem);

            // Act
            var result = signer.SignLicenseDocument(license);

            // Assert
            Assert.Same(license, result);
        }

        [Fact]
        public void SignLicenseDocument_ProducesValidBase64Signature()
        {
            // Arrange
            var license = CreateTestLicense();
            var signer = new LicenseSigner(_privateKeyPem);

            // Act
            signer.SignLicenseDocument(license);

            // Assert
            var signature = license["Signature"]!.ToString()!;
            byte[] signatureBytes = null!;
            var exception = Record.Exception(() => signatureBytes = Convert.FromBase64String(signature));
            Assert.Null(exception);
            Assert.NotNull(signatureBytes);
            Assert.True(signatureBytes.Length > 0);
        }

        [Fact]
        public void SignLicenseDocument_ThrowsWhenLicenseIsNull()
        {
            // Arrange
            var signer = new LicenseSigner(_privateKeyPem);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => signer.SignLicenseDocument(null!));
        }

        [Fact]
        public void SignLicenseDocument_WithPkcs1Padding_CreatesValidSignature()
        {
            // Arrange
            var license = CreateTestLicense();
            var signer = new LicenseSigner(_privateKeyPem, PaddingChoice.Pkcs1);

            // Act
            signer.SignLicenseDocument(license);

            // Assert
            Assert.NotNull(license["Signature"]);
            
            // Verify with matching padding
            var verifier = new LicenseVerifier(_publicKeyPem, PaddingChoice.Pkcs1);
            var isValid = verifier.VerifyLicenseDocument(license, out var _);
            Assert.True(isValid);
        }

        [Fact]
        public void SignLicenseDocument_RestoresOriginalSignatureOnError()
        {
            // Arrange
            var license = CreateTestLicense();
            license["Signature"] = "original-sig";
            var invalidPrivateKey = "INVALID KEY";
            var signer = new LicenseSigner(invalidPrivateKey);

            // Act & Assert
            Assert.ThrowsAny<Exception>(() => signer.SignLicenseDocument(license));
            Assert.Equal("original-sig", license["Signature"]);
        }

        // ═══════════════════════════════════════════════════════
        // VERIFICATION TESTS
        // ═══════════════════════════════════════════════════════

        [Fact]
        public void VerifyLicenseDocument_ValidSignature_ReturnsTrue()
        {
            // Arrange
            var license = CreateTestLicense();
            var signer = new LicenseSigner(_privateKeyPem);
            signer.SignLicenseDocument(license);
            var verifier = new LicenseVerifier(_publicKeyPem);

            // Act
            var isValid = verifier.VerifyLicenseDocument(license, out var failureReason);

            // Assert
            Assert.True(isValid);
            Assert.Null(failureReason);
        }

        [Fact]
        public void VerifyLicenseDocument_NullLicense_ReturnsFalse()
        {
            // Arrange
            var verifier = new LicenseVerifier(_publicKeyPem);

            // Act
            var isValid = verifier.VerifyLicenseDocument(null!, out var failureReason);

            // Assert
            Assert.False(isValid);
            Assert.Equal("License is null", failureReason);
        }

        [Fact]
        public void VerifyLicenseDocument_MissingSignature_ReturnsFalse()
        {
            // Arrange
            var license = CreateTestLicense();
            var verifier = new LicenseVerifier(_publicKeyPem);

            // Act
            var isValid = verifier.VerifyLicenseDocument(license, out var failureReason);

            // Assert
            Assert.False(isValid);
            Assert.Equal("Signature missing or empty", failureReason);
        }

        [Fact]
        public void VerifyLicenseDocument_InvalidBase64Signature_ReturnsFalse()
        {
            // Arrange
            var license = CreateTestLicense();
            license["Signature"] = "not-valid-base64!!!";
            var verifier = new LicenseVerifier(_publicKeyPem);

            // Act
            var isValid = verifier.VerifyLicenseDocument(license, out var failureReason);

            // Assert
            Assert.False(isValid);
            Assert.Equal("Signature is not valid Base64", failureReason);
        }

        [Fact]
        public void VerifyLicenseDocument_TamperedField_ReturnsFalse()
        {
            // Arrange
            var license = CreateTestLicense();
            var signer = new LicenseSigner(_privateKeyPem);
            signer.SignLicenseDocument(license);

            // Tamper with license
            license["MaxUsers"] = 9999;

            var verifier = new LicenseVerifier(_publicKeyPem);

            // Act
            var isValid = verifier.VerifyLicenseDocument(license, out var failureReason);

            // Assert
            Assert.False(isValid);
            Assert.Equal("Signature verification failed", failureReason);
        }

        [Fact]
        public void VerifyLicenseDocument_TamperedCustomerName_ReturnsFalse()
        {
            // Arrange
            var license = CreateTestLicense();
            var signer = new LicenseSigner(_privateKeyPem);
            signer.SignLicenseDocument(license);

            // Tamper with customer name
            license["CustomerName"] = "Hacker Corp";

            var verifier = new LicenseVerifier(_publicKeyPem);

            // Act
            var isValid = verifier.VerifyLicenseDocument(license, out var failureReason);

            // Assert
            Assert.False(isValid);
            Assert.Contains("Signature verification failed", failureReason);
        }

        [Fact]
        public void VerifyLicenseDocument_WrongPublicKey_ReturnsFalse()
        {
            // Arrange
            var license = CreateTestLicense();
            var signer = new LicenseSigner(_privateKeyPem);
            signer.SignLicenseDocument(license);

            // Use different public key
            string wrongPublicKey;
            using (var rsa = RSA.Create(2048))
            {
                wrongPublicKey = rsa.ExportRSAPublicKeyPem();
            }

            var verifier = new LicenseVerifier(wrongPublicKey);

            // Act
            var isValid = verifier.VerifyLicenseDocument(license, out var failureReason);

            // Assert
            Assert.False(isValid);
            Assert.Equal("Signature verification failed", failureReason);
        }

        [Fact]
        public void VerifyLicenseDocument_PreservesOriginalSignature()
        {
            // Arrange
            var license = CreateTestLicense();
            var signer = new LicenseSigner(_privateKeyPem);
            signer.SignLicenseDocument(license);
            var originalSignature = license["Signature"];
            var verifier = new LicenseVerifier(_publicKeyPem);

            // Act
            verifier.VerifyLicenseDocument(license, out var _);

            // Assert
            Assert.Equal(originalSignature, license["Signature"]);
        }

        [Fact]
        public void VerifyLicenseDocument_PaddingMismatch_ReturnsFalse()
        {
            // Arrange
            var license = CreateTestLicense();
            var signer = new LicenseSigner(_privateKeyPem, PaddingChoice.Pss);
            signer.SignLicenseDocument(license);

            // Verify with different padding
            var verifier = new LicenseVerifier(_publicKeyPem, PaddingChoice.Pkcs1);

            // Act
            var isValid = verifier.VerifyLicenseDocument(license, out var failureReason);

            // Assert
            Assert.False(isValid);
            Assert.Equal("Signature verification failed", failureReason);
        }

        // ═══════════════════════════════════════════════════════
        // JSON VERIFICATION TESTS
        // ═══════════════════════════════════════════════════════

        [Fact]
        public void VerifyLicenseDocumentJson_ValidSignature_ReturnsTrue()
        {
            // Arrange
            var license = CreateTestLicense();
            var signer = new LicenseSigner(_privateKeyPem);
            signer.SignLicenseDocument(license);
            var json = license.ToJson();
            var verifier = new LicenseVerifier(_publicKeyPem);

            // Act
            var isValid = verifier.VerifyLicenseDocumentJson(json, out var failureReason);

            // Assert
            Assert.True(isValid);
            Assert.Null(failureReason);
        }

        [Fact]
        public void VerifyLicenseDocumentJson_EmptyJson_ReturnsFalse()
        {
            // Arrange
            var verifier = new LicenseVerifier(_publicKeyPem);

            // Act
            var isValid = verifier.VerifyLicenseDocumentJson("", out var failureReason);

            // Assert
            Assert.False(isValid);
            Assert.Equal("Empty JSON", failureReason);
        }

        [Fact]
        public void VerifyLicenseDocumentJson_InvalidJson_ReturnsFalse()
        {
            // Arrange
            var verifier = new LicenseVerifier(_publicKeyPem);

            // Act
            var isValid = verifier.VerifyLicenseDocumentJson("{invalid json", out var failureReason);

            // Assert
            Assert.False(isValid);
            Assert.Contains("Invalid JSON", failureReason);
        }

        [Fact]
        public void VerifyLicenseDocumentJson_TamperedJson_ReturnsFalse()
        {
            // Arrange
            var license = CreateTestLicense();
            var signer = new LicenseSigner(_privateKeyPem);
            signer.SignLicenseDocument(license);
            var json = license.ToJson();

            // Tamper with JSON
            var tamperedJson = json.Replace("\"MaxUsers\": 10", "\"MaxUsers\": 9999");

            var verifier = new LicenseVerifier(_publicKeyPem);

            // Act
            var isValid = verifier.VerifyLicenseDocumentJson(tamperedJson, out var failureReason);

            // Assert
            Assert.False(isValid);
            Assert.Equal("Signature verification failed", failureReason);
        }

        // ═══════════════════════════════════════════════════════
        // CANONICAL SERIALIZATION TESTS
        // ═══════════════════════════════════════════════════════

        [Fact]
        public void CanonicalSerialization_ProducesSameBytes_ForSameLicense()
        {
            // Arrange
            var license = CreateTestLicense();
            var serializer = new CanonicalLicenseSerializer();

            // Act
            var bytes1 = serializer.SerializeLicenseDocument(license);
            var bytes2 = serializer.SerializeLicenseDocument(license);

            // Assert
            Assert.Equal(bytes1, bytes2);
        }

        [Fact]
        public void CanonicalSerialization_ProducesSameBytes_AfterRoundtrip()
        {
            // Arrange
            var license = CreateTestLicense();
            var serializer = new CanonicalLicenseSerializer();
            var bytes1 = serializer.SerializeLicenseDocument(license);

            // Roundtrip through JSON
            var json = license.ToJson();
            var reloaded = License.FromJson(json);
            reloaded["Signature"] = null; // Ensure no signature for comparison

            // Act
            var bytes2 = serializer.SerializeLicenseDocument(reloaded);

            // Assert
            Assert.Equal(bytes1, bytes2);
        }

        [Fact]
        public void CanonicalSerialization_ExcludesSignatureField()
        {
            // Arrange
            var license = CreateTestLicense();
            license["Signature"] = "test-signature";
            var serializer = new CanonicalLicenseSerializer();

            // Act
            var bytes = serializer.SerializeLicenseDocument(license);
            var json = System.Text.Encoding.UTF8.GetString(bytes);

            // Assert
            Assert.DoesNotContain("Signature", json);
            Assert.DoesNotContain("test-signature", json);
        }

        [Fact]
        public void CanonicalSerialization_ExcludesUnSerializedFields()
        {
            // Arrange
            var license = CreateTestLicense();
            license["CustomInfo"] = "should be excluded";
            var serializer = new CanonicalLicenseSerializer
            {
                UnSerializedFields = new List<string> { "CustomInfo" }
            };

            // Act
            var bytes = serializer.SerializeLicenseDocument(license);
            var json = System.Text.Encoding.UTF8.GetString(bytes);

            // Assert
            Assert.DoesNotContain("CustomInfo", json);
            Assert.DoesNotContain("should be excluded", json);
        }

        // ═══════════════════════════════════════════════════════
        // INTEGRATION TESTS
        // ═══════════════════════════════════════════════════════

        [Fact]
        public void FullWorkflow_CreateSignVerify_Success()
        {
            // Arrange
            var creator = new LicenseCreator();
            var signer = new LicenseSigner(_privateKeyPem);
            var verifier = new LicenseVerifier(_publicKeyPem);

            // Act - Create
            var license = creator.CreateLicense(_testSchema, new Dictionary<string, object?>
            {
                ["LicenseId"] = "FULL-TEST-001",
                ["CustomerName"] = "Integration Test Corp",
                ["ExpiryUtc"] = DateTime.UtcNow.AddYears(1),
                ["MaxUsers"] = 100
            });

            // Act - Sign
            signer.SignLicenseDocument(license);

            // Act - Verify
            var isValid = verifier.VerifyLicenseDocument(license, out var failureReason);

            // Assert
            Assert.True(isValid);
            Assert.Null(failureReason);
            Assert.NotNull(license["Signature"]);
        }

        [Fact]
        public void FullWorkflow_SerializeDeserializeVerify_Success()
        {
            // Arrange
            var creator = new LicenseCreator();
            var signer = new LicenseSigner(_privateKeyPem);
            var verifier = new LicenseVerifier(_publicKeyPem);

            var license = creator.CreateLicense(_testSchema, new Dictionary<string, object?>
            {
                ["LicenseId"] = "SER-TEST-001",
                ["CustomerName"] = "Serialization Test Corp",
                ["ExpiryUtc"] = DateTime.UtcNow.AddYears(1),
                ["MaxUsers"] = 50
            });

            signer.SignLicenseDocument(license);

            // Act - Serialize to JSON
            var json = license.ToJson();

            // Act - Deserialize from JSON
            var reloaded = License.FromJson(json);

            // Act - Verify reloaded license
            var isValid = verifier.VerifyLicenseDocument(reloaded, out var failureReason);

            // Assert
            Assert.True(isValid);
            Assert.Null(failureReason);
        }

        [Fact]
        public void FullWorkflow_MultipleFields_TamperDetection()
        {
            // Arrange - Create comprehensive license
            var schema = new LicenseSchema(
                "ComprehensiveLicense",
                new List<FieldDescriptor>
                {
                    new("LicenseId", "string", Required: true),
                    new("CustomerName", "string", Required: true),
                    new("ExpiryUtc", "datetime", Required: true),
                    new("MaxUsers", "int", Required: true),
                    new("Features", "list<string>", Required: false),
                    new("Tier", "string", Required: false),
                    new("Signature", "string", Required: false)
                }
            );

            var creator = new LicenseCreator();
            var license = creator.CreateLicense(schema, new Dictionary<string, object?>
            {
                ["LicenseId"] = "COMP-001",
                ["CustomerName"] = "Test Company",
                ["ExpiryUtc"] = DateTime.UtcNow.AddYears(1),
                ["MaxUsers"] = 25,
                ["Features"] = new List<string> { "Feature1", "Feature2" },
                ["Tier"] = "Premium"
            });

            var signer = new LicenseSigner(_privateKeyPem);
            signer.SignLicenseDocument(license);
            var verifier = new LicenseVerifier(_publicKeyPem);

            // Act & Assert - Test each field for tamper detection
            var fieldsToTest = new Dictionary<string, object?>
            {
                ["MaxUsers"] = 9999,
                ["CustomerName"] = "Evil Corp",
                ["Tier"] = "Ultimate"
            };

            foreach (var field in fieldsToTest)
            {
                var tamperedLicense = License.FromJson(license.ToJson());
                tamperedLicense[field.Key] = field.Value;

                var isValid = verifier.VerifyLicenseDocument(tamperedLicense, out var reason);
                
                Assert.False(isValid, $"Tampering {field.Key} should be detected");
                Assert.Equal("Signature verification failed", reason);
            }
        }
    }
}
