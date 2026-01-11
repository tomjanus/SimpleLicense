using Xunit;
using Shouldly;
using SimpleLicense.Core;
using SimpleLicense.Core.LicenseValidation;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleLicense.Tests
{
    /// <summary>
    /// Tests for LicenseSerializer - JSON serialization/deserialization for License and License
    /// </summary>
    public class LicenseSerializerTests
    {
        // ============================================================================
        // License Tests
        // ============================================================================

        [Fact]
        public void SerializeLicenseDocument_WithValidDocument_ShouldReturnJson()
        {
            // Arrange
            var serializer = new LicenseSerializer();
            var doc = new License(ensureMandatoryKeys: true);
            doc["LicenseId"] = "DOC-001";
            doc["ExpiryUtc"] = DateTime.UtcNow.AddMonths(6);
            doc["Signature"] = "test-signature";
            doc["CustomField"] = "custom-value";

            // Act
            var json = serializer.SerializeLicenseDocument(doc, validate: false);

            // Assert
            json.ShouldNotBeNullOrEmpty();
            json.ShouldContain("DOC-001");
            json.ShouldContain("custom-value");
        }

        [Fact]
        public void SerializeLicenseDocument_WithValidation_ShouldValidateMandatoryFields()
        {
            // Arrange
            var serializer = new LicenseSerializer();
            var doc = new License(ensureMandatoryKeys: true);
            doc["LicenseId"] = "VALID-001";
            doc["ExpiryUtc"] = DateTime.UtcNow.AddMonths(12);
            doc["Signature"] = "valid-signature";

            // Act & Assert
            Should.NotThrow(() => serializer.SerializeLicenseDocument(doc, validate: true));
        }

        [Fact]
        public void SerializeLicenseDocument_WithInvalidDocument_ShouldThrow()
        {
            // Arrange
            var serializer = new LicenseSerializer();
            var doc = new License(ensureMandatoryKeys: false);
            // Missing mandatory fields

            // Act & Assert
            Should.Throw<LicenseValidationException>(() => 
                serializer.SerializeLicenseDocument(doc, validate: true));
        }

        [Fact]
        public void DeserializeLicenseDocument_WithValidJson_ShouldRecreateDocument()
        {
            // Arrange
            var serializer = new LicenseSerializer();
            var json = @"{
                ""LicenseId"": ""DESER-001"",
                ""ExpiryUtc"": ""2027-12-31T23:59:59Z"",
                ""Signature"": ""sig-123"",
                ""CompanyName"": ""Test Corp"",
                ""MaxUsers"": 100
            }";

            // Act
            var doc = serializer.DeserializeLicenseDocument(json);

            // Assert
            doc["LicenseId"].ShouldBe("DESER-001");
            doc["CompanyName"].ShouldBe("Test Corp");
            doc["MaxUsers"].ShouldBe(100L); // JSON deserializes numbers as Int64
        }

        [Fact]
        public void SerializeDeserialize_RoundTrip_ShouldPreserveData()
        {
            // Arrange
            var serializer = new LicenseSerializer();
            var original = new License(ensureMandatoryKeys: true);
            original["LicenseId"] = "ROUND-TRIP-001";
            original["ExpiryUtc"] = new DateTime(2027, 3, 15, 10, 30, 0, DateTimeKind.Utc);
            original["Signature"] = "original-signature";
            original["Tier"] = "Enterprise";
            original["MaxUsers"] = 250;
            original["Features"] = new List<string> { "Feature1", "Feature2", "Feature3" };

            // Act
            var json = serializer.SerializeLicenseDocument(original);
            var recovered = serializer.DeserializeLicenseDocument(json);

            // Assert
            recovered["LicenseId"].ShouldBe(original["LicenseId"]);
            recovered["Tier"].ShouldBe(original["Tier"]);
            recovered["MaxUsers"].ShouldBe(250L); // Numbers become Int64 in JSON
            recovered["Signature"].ShouldBe(original["Signature"]);
            
            var recoveredFeatures = recovered["Features"] as List<object>;
            recoveredFeatures.ShouldNotBeNull();
            recoveredFeatures.Count.ShouldBe(3);
        }

        [Fact]
        public void SerializeLicenseDocumentToBytes_ShouldReturnValidUtf8()
        {
            // Arrange
            var serializer = new LicenseSerializer();
            var doc = new License(ensureMandatoryKeys: true);
            doc["LicenseId"] = "BYTES-001";
            doc["ExpiryUtc"] = DateTime.UtcNow.AddYears(1);
            doc["Signature"] = "bytes-signature";

            // Act
            var bytes = serializer.SerializeLicenseDocumentToBytes(doc);

            // Assert
            bytes.ShouldNotBeNull();
            bytes.Length.ShouldBeGreaterThan(0);
            
            // Should be valid UTF-8
            var reconstructed = System.Text.Encoding.UTF8.GetString(bytes);
            reconstructed.ShouldContain("BYTES-001");
        }

        [Fact]
        public void DeserializeLicenseDocumentFromBytes_ShouldRecreateDocument()
        {
            // Arrange
            var serializer = new LicenseSerializer();
            var original = new License(ensureMandatoryKeys: true);
            original["LicenseId"] = "BYTES-ROUND-001";
            original["ExpiryUtc"] = DateTime.UtcNow.AddYears(1);
            original["Signature"] = "bytes-round-signature";
            original["CustomData"] = "test-value";

            // Act
            var bytes = serializer.SerializeLicenseDocumentToBytes(original);
            var recovered = serializer.DeserializeLicenseDocumentFromBytes(bytes);

            // Assert
            recovered["LicenseId"].ShouldBe(original["LicenseId"]);
            recovered["CustomData"].ShouldBe(original["CustomData"]);
            recovered["Signature"].ShouldBe(original["Signature"]);
        }

        [Fact]
        public void SerializeLicenseDocument_WithNullLicense_ShouldThrow()
        {
            // Arrange
            var serializer = new LicenseSerializer();
            License? nullDoc = null;

            // Act & Assert
            Should.Throw<ArgumentNullException>(() => 
                serializer.SerializeLicenseDocument(nullDoc!));
        }

        [Fact]
        public void DeserializeLicenseDocument_WithNullJson_ShouldThrow()
        {
            // Arrange
            var serializer = new LicenseSerializer();
            string? nullJson = null;

            // Act & Assert
            Should.Throw<ArgumentNullException>(() => 
                serializer.DeserializeLicenseDocument(nullJson!));
        }

        [Fact]
        public void DeserializeLicenseDocument_WithInvalidJson_ShouldThrow()
        {
            // Arrange
            var serializer = new LicenseSerializer();
            var invalidJson = "{ invalid json }";

            // Act & Assert
            Should.Throw<InvalidOperationException>(() => 
                serializer.DeserializeLicenseDocument(invalidJson));
        }

        [Fact]
        public void DeserializeLicenseDocumentFromBytes_WithNullBytes_ShouldThrow()
        {
            // Arrange
            var serializer = new LicenseSerializer();
            byte[]? nullBytes = null;

            // Act & Assert
            Should.Throw<ArgumentNullException>(() => 
                serializer.DeserializeLicenseDocumentFromBytes(nullBytes!));
        }

        [Fact]
        public void SerializeLicenseDocument_WithComplexNestedData_ShouldPreserveStructure()
        {
            // Arrange
            var serializer = new LicenseSerializer();
            var doc = new License(ensureMandatoryKeys: true);
            doc["LicenseId"] = "COMPLEX-001";
            doc["ExpiryUtc"] = DateTime.UtcNow.AddYears(1);
            doc["Signature"] = "complex-signature";
            doc["Metadata"] = new Dictionary<string, object?>
            {
                ["Version"] = "2.0",
                ["CreatedBy"] = "System",
                ["Tags"] = new List<string> { "production", "enterprise" }
            };

            // Act
            var json = serializer.SerializeLicenseDocument(doc);
            var recovered = serializer.DeserializeLicenseDocument(json);

            // Assert
            recovered["LicenseId"].ShouldBe("COMPLEX-001");
            var metadata = recovered["Metadata"];
            metadata.ShouldNotBeNull();
        }

        [Fact]
        public void JsonProfiles_Canonical_ShouldHaveCorrectSettings()
        {
            // Assert
            JsonProfiles.Canonical.PropertyNamingPolicy.ShouldBe(JsonNamingPolicy.CamelCase);
            JsonProfiles.Canonical.DefaultIgnoreCondition.ShouldBe(JsonIgnoreCondition.Never);
            JsonProfiles.Canonical.WriteIndented.ShouldBeFalse();
        }

        [Fact]
        public void JsonProfiles_Pretty_ShouldHaveCorrectSettings()
        {
            // Assert
            JsonProfiles.Pretty.PropertyNamingPolicy.ShouldBe(JsonNamingPolicy.CamelCase);
            JsonProfiles.Pretty.DefaultIgnoreCondition.ShouldBe(JsonIgnoreCondition.Never);
            JsonProfiles.Pretty.WriteIndented.ShouldBeTrue();
        }
    }
}
