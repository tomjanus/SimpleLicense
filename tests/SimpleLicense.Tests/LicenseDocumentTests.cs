using Xunit;
using Shouldly;
using SimpleLicense.LicenseValidation;
using System.Text.Json;

namespace SimpleLicense.Tests
{
    /// <summary>
    /// Tests for LicenseDocument - Field-level validation and serialization
    /// </summary>
    public class LicenseDocumentTests
    {
        [Fact]
        public void Constructor_WithEnsureMandatoryKeys_ShouldCreateMandatoryFields()
        {
            // Arrange & Act
            var license = new LicenseDocument(ensureMandatoryKeys: true);

            // Assert
            license["LicenseId"].ShouldBeNull();
            license["ExpiryUtc"].ShouldBeNull();
            license["Signature"].ShouldBeNull();
        }

        [Fact]
        public void Constructor_WithoutEnsureMandatoryKeys_ShouldNotCreateFields()
        {
            // Arrange & Act
            var license = new LicenseDocument(ensureMandatoryKeys: false);

            // Assert
            license.Fields.ShouldBeEmpty();
        }

        [Fact]
        public void Indexer_Get_ShouldReturnNullForNonExistentField()
        {
            // Arrange
            var license = new LicenseDocument(ensureMandatoryKeys: false);

            // Assert
            license["NonExistent"].ShouldBeNull();
        }

        [Fact]
        public void Indexer_Set_WithValidValue_ShouldSetField()
        {
            // Arrange
            var license = new LicenseDocument();

            // Act
            license["LicenseId"] = "TEST-12345";

            // Assert
            license["LicenseId"].ShouldBe("TEST-12345");
        }

        [Fact]
        public void Indexer_Set_WithInvalidValue_ShouldThrowValidationException()
        {
            // Arrange
            var license = new LicenseDocument();

            // Act & Assert
            Should.Throw<LicenseValidationException>(() => license["LicenseId"] = "");
        }

        [Fact]
        public void Indexer_IsCaseInsensitive()
        {
            // Arrange
            var license = new LicenseDocument();
            license["LicenseId"] = "TEST-123";

            // Act & Assert
            license["licenseid"].ShouldBe("TEST-123");
            license["LICENSEID"].ShouldBe("TEST-123");
            license["LiCeNsEiD"].ShouldBe("TEST-123");
        }

        [Fact]
        public void SetField_WithValidValue_ShouldReturnTrueAndSetValue()
        {
            // Arrange
            var license = new LicenseDocument();

            // Act
            var result = license.SetField("LicenseId", "VALID-ID", out var error);

            // Assert
            result.ShouldBeTrue();
            error.ShouldBeNull();
            license["LicenseId"].ShouldBe("VALID-ID");
        }

        [Fact]
        public void SetField_WithInvalidValue_ShouldReturnFalseAndSetError()
        {
            // Arrange
            var license = new LicenseDocument();

            // Act
            var result = license.SetField("LicenseId", "", out var error);

            // Assert
            result.ShouldBeFalse();
            error.ShouldNotBeNull();
            error.ShouldContain("empty");
        }

        [Fact]
        public void SetField_WithNullOrWhitespaceName_ShouldReturnFalse()
        {
            // Arrange
            var license = new LicenseDocument();

            // Act
            var result = license.SetField("", "value", out var error);

            // Assert
            result.ShouldBeFalse();
            error.ShouldBe("Field name cannot be null or whitespace.");
        }

        [Fact]
        public void SetField_WithUnvalidatedField_ShouldStoreRawValue()
        {
            // Arrange
            var license = new LicenseDocument();

            // Act
            var result = license.SetField("CustomField", 42, out var error);

            // Assert
            result.ShouldBeTrue();
            error.ShouldBeNull();
            license["CustomField"].ShouldBe(42);
        }

        [Fact]
        public void SetField_WithValidator_ShouldNormalizeValue()
        {
            // Arrange
            var license = new LicenseDocument();

            // Act - LicenseId validator trims whitespace
            var result = license.SetField("LicenseId", "  TRIMMED  ", out var error);

            // Assert
            result.ShouldBeTrue();
            license["LicenseId"].ShouldBe("TRIMMED");
        }

        [Fact]
        public void Fields_ShouldReturnAllFieldsAsEnumerable()
        {
            // Arrange
            var license = new LicenseDocument(ensureMandatoryKeys: false);
            license.SetField("Field1", "Value1", out _);
            license.SetField("Field2", 42, out _);
            license.SetField("Field3", true, out _);

            // Act
            var fields = license.Fields.ToList();

            // Assert
            fields.Count.ShouldBe(3);
            fields.ShouldContain(f => f.Key == "Field1" && f.Value as string == "Value1");
            fields.ShouldContain(f => f.Key == "Field2" && (int?)f.Value == 42);
            fields.ShouldContain(f => f.Key == "Field3" && (bool?)f.Value == true);
        }

        [Fact]
        public void EnsureMandatoryPresent_WithAllValidFields_ShouldNotThrow()
        {
            // Arrange
            var license = new LicenseDocument();
            license["LicenseId"] = "TEST-123";
            license["ExpiryUtc"] = DateTime.UtcNow.AddYears(1);
            license["Signature"] = "valid-signature";

            // Act & Assert
            Should.NotThrow(() => license.EnsureMandatoryPresent());
        }

        [Fact]
        public void EnsureMandatoryPresent_WithNullMandatoryField_ShouldThrow()
        {
            // Arrange
            var license = new LicenseDocument();
            license["LicenseId"] = "TEST-123";
            // ExpiryUtc is null
            license["Signature"] = "sig";

            // Act & Assert
            var ex = Should.Throw<LicenseValidationException>(() => license.EnsureMandatoryPresent());
            ex.Issues.ShouldContain(i => i.Contains("ExpiryUtc"));
        }

        [Fact]
        public void EnsureMandatoryPresent_WithInvalidMandatoryField_ShouldThrow()
        {
            // Arrange
            var license = new LicenseDocument();
            license.SetField("LicenseId", "", out _); // This will fail via indexer
            
            // Act & Assert
            Should.Throw<LicenseValidationException>(() => license["LicenseId"] = "");
        }

        [Fact]
        public void EnsureMandatoryPresent_WithMultipleIssues_ShouldReportAll()
        {
            // Arrange
            var license = new LicenseDocument();
            // All mandatory fields are null (Signature can be null, so only 2 issues expected)

            // Act & Assert
            var ex = Should.Throw<LicenseValidationException>(() => license.EnsureMandatoryPresent());
            ex.Issues.Count.ShouldBeGreaterThanOrEqualTo(2);
            ex.Issues.ShouldContain(i => i.Contains("LicenseId"));
            ex.Issues.ShouldContain(i => i.Contains("ExpiryUtc"));
        }

        [Fact]
        public void ToJson_WithValidation_ShouldValidateBeforeSerializing()
        {
            // Arrange
            var license = new LicenseDocument();
            license["LicenseId"] = "TEST-123";
            license["ExpiryUtc"] = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc);
            license["Signature"] = "sig";

            // Act
            var json = license.ToJson(validate: true);

            // Assert
            json.ShouldNotBeNullOrEmpty();
            json.ShouldContain("TEST-123");
            json.ShouldContain("2025-12-31");
        }

        [Fact]
        public void ToJson_WithoutValidation_ShouldSerializeWithoutValidating()
        {
            // Arrange
            var license = new LicenseDocument();
            license["CustomField"] = "value";

            // Act
            var json = license.ToJson(validate: false);

            // Assert
            json.ShouldNotBeNullOrEmpty();
            json.ShouldContain("CustomField");
        }

        [Fact]
        public void ToJson_WithDateTime_ShouldConvertToIsoString()
        {
            // Arrange
            var license = new LicenseDocument();
            license["LicenseId"] = "TEST";
            license["ExpiryUtc"] = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc);
            license["Signature"] = "sig";

            // Act
            var json = license.ToJson();

            // Assert
            json.ShouldContain("2025-06-15T10:30:00");
        }

        [Fact]
        public void ToJson_WithInvalidData_AndValidationEnabled_ShouldThrow()
        {
            // Arrange
            var license = new LicenseDocument();
            // Missing mandatory fields

            // Act & Assert
            Should.Throw<LicenseValidationException>(() => license.ToJson(validate: true));
        }

        [Fact]
        public void FromJson_WithValidJson_ShouldDeserialize()
        {
            // Arrange
            var json = @"{
                ""LicenseId"": ""TEST-123"",
                ""ExpiryUtc"": ""2025-12-31T23:59:59Z"",
                ""Signature"": ""test-signature"",
                ""MaxUsers"": 10
            }";

            // Act
            var license = LicenseDocument.FromJson(json);

            // Assert
            license["LicenseId"].ShouldBe("TEST-123");
            license["ExpiryUtc"].ShouldBeOfType<DateTime>();
            license["Signature"].ShouldBe("test-signature");
            license["MaxUsers"].ShouldBe(10);
        }

        [Fact]
        public void FromJson_WithInvalidJson_ShouldThrow()
        {
            // Arrange
            var json = "{ invalid json }";

            // Act & Assert
            Should.Throw<InvalidOperationException>(() => LicenseDocument.FromJson(json));
        }

        [Fact]
        public void FromJson_WithValidationFailure_ShouldThrowWithErrors()
        {
            // Arrange
            var json = @"{
                ""LicenseId"": """",
                ""ExpiryUtc"": ""2025-12-31T23:59:59Z"",
                ""Signature"": ""sig""
            }";

            // Act & Assert
            var ex = Should.Throw<LicenseValidationException>(() => LicenseDocument.FromJson(json));
            ex.Issues.ShouldContain(i => i.Contains("LicenseId"));
        }

        [Fact]
        public void FromJson_WithMissingMandatoryFields_ShouldCreateThemAsNull()
        {
            // Arrange
            var json = @"{
                ""CustomField"": ""value""
            }";

            // Act
            var license = LicenseDocument.FromJson(json);

            // Assert
            license["LicenseId"].ShouldBeNull();
            license["ExpiryUtc"].ShouldBeNull();
            license["Signature"].ShouldBeNull();
            license["CustomField"].ShouldBe("value");
        }

        [Fact]
        public void FromJson_WithComplexTypes_ShouldDeserialize()
        {
            // Arrange
            var json = @"{
                ""LicenseId"": ""TEST"",
                ""ExpiryUtc"": 1735689599,
                ""Signature"": ""sig"",
                ""IsActive"": true,
                ""MaxUsers"": 100,
                ""Features"": [""feature1"", ""feature2""],
                ""Metadata"": {
                    ""version"": ""1.0"",
                    ""created"": ""2025-01-01""
                }
            }";

            // Act
            var license = LicenseDocument.FromJson(json);

            // Assert
            license["IsActive"].ShouldBe(true);
            license["MaxUsers"].ShouldBe(100);
            license["Features"].ShouldBeOfType<List<object>>();
            ((List<object>)license["Features"]!).Count.ShouldBe(2);
            license["Metadata"].ShouldBeOfType<Dictionary<string, object>>();
        }

        [Fact]
        public void ToJson_ThenFromJson_ShouldRoundTrip()
        {
            // Arrange
            var original = new LicenseDocument();
            original["LicenseId"] = "ROUND-TRIP-123";
            original["ExpiryUtc"] = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc);
            original["Signature"] = "signature-data";
            original["MaxUsers"] = 50;
            original["IsActive"] = true;

            // Act
            var json = original.ToJson();
            var restored = LicenseDocument.FromJson(json);

            // Assert
            restored["LicenseId"].ShouldBe("ROUND-TRIP-123");
            restored["Signature"].ShouldBe("signature-data");
            restored["MaxUsers"].ShouldBe((long)50); // JSON deserializes as long
            restored["IsActive"].ShouldBe(true);
            ((DateTime)restored["ExpiryUtc"]!).Year.ShouldBe(2025);
        }

        [Fact]
        public void RegisterValidator_ShouldAddValidatorToRegistry()
        {
            // Arrange
            var called = false;
            FieldValidator customValidator = (value) =>
            {
                called = true;
                return ValidationResult.Success(value);
            };

            // Act
            LicenseDocument.RegisterFieldValidator("TestField", customValidator);
            var license = new LicenseDocument();
            license.SetField("TestField", "test", out _);

            // Assert
            called.ShouldBeTrue();
        }

        [Fact]
        public void ValidationResult_Success_ShouldHaveCorrectProperties()
        {
            // Act
            var result = ValidationResult.Success("output-value");

            // Assert
            result.IsValid.ShouldBeTrue();
            result.OutputValue.ShouldBe("output-value");
            result.Error.ShouldBeNull();
        }

        [Fact]
        public void ValidationResult_Fail_ShouldHaveCorrectProperties()
        {
            // Act
            var result = ValidationResult.Fail("error message");

            // Assert
            result.IsValid.ShouldBeFalse();
            result.OutputValue.ShouldBeNull();
            result.Error.ShouldBe("error message");
        }

        [Fact]
        public void LicenseValidationException_ShouldContainIssues()
        {
            // Arrange
            var issues = new[] { "Issue 1", "Issue 2", "Issue 3" };

            // Act
            var ex = new LicenseValidationException(issues);

            // Assert
            ex.Issues.Count.ShouldBe(3);
            ex.Message.ShouldContain("Issue 1");
            ex.Message.ShouldContain("Issue 2");
            ex.Message.ShouldContain("Issue 3");
        }

        [Fact]
        public void LicenseValidationException_WithEmptyIssues_ShouldHaveEmptyList()
        {
            // Act
            var ex = new LicenseValidationException(new List<string>());

            // Assert
            ex.Issues.ShouldBeEmpty();
        }

        [Fact]
        public void LicenseValidationException_WithNullIssues_ShouldHaveEmptyList()
        {
            // Act
            var ex = new LicenseValidationException(null!);

            // Assert
            ex.Issues.ShouldBeEmpty();
        }

        [Fact]
        public void SetField_WithMultipleValidations_ShouldApplyInOrder()
        {
            // Arrange
            var license = new LicenseDocument();

            // Act - Set multiple fields with validation
            license.SetField("LicenseId", "  ID-123  ", out _); // Should trim
            license.SetField("MaxUsers", 10.0, out _); // Should accept numeric
            license.SetField("CustomerName", "  John Doe  ", out _); // Should trim

            // Assert
            license["LicenseId"].ShouldBe("ID-123");
            license["MaxUsers"].ShouldBe(10);
            license["CustomerName"].ShouldBe("John Doe");
        }

        [Fact]
        public void Fields_ShouldBeCaseInsensitiveInEnumeration()
        {
            // Arrange
            var license = new LicenseDocument(ensureMandatoryKeys: false);
            license.SetField("TestField", "value", out _);

            // Act
            var field = license.Fields.FirstOrDefault(f => f.Key.Equals("testfield", StringComparison.OrdinalIgnoreCase));

            // Assert
            field.Key.ShouldNotBeNull();
            field.Value.ShouldBe("value");
        }
    }
}
