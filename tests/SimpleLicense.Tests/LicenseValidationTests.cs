using Xunit;
using Shouldly;
using SimpleLicense.Core;
using SimpleLicense.Core.LicenseValidation;

namespace SimpleLicense.Tests
{
    /// <summary>
    /// Tests for LicenseValidator - Schema-level validation
    /// </summary>
    public class LicenseValidatorTests
    {
        private readonly LicenseSchema _demoSchema;
        private readonly LicenseValidator _validator;

        public LicenseValidatorTests()
        {
            // Create schema once for all tests
            _demoSchema = new LicenseSchema(
                "DemoSchema",
                new List<FieldDescriptor>
                {
                    new("LicenseId", "string", Signed: true, Required: true),
                    new("ExpiryUtc", "datetime", Signed: true, Required: true),
                    new("Signature", "string", Signed: false, Required: true),
                    new("CustomerName", "string", Signed: true, Required: true),
                    new("MaxUsers", "int", Signed: true, Required: false),
                    new("Features", "list<string>", Signed: true, Required: false),
                    new("IsActive", "bool", Signed: true, Required: false, DefaultValue: true)
                }
            );
            _validator = new LicenseValidator(_demoSchema);
        }

        [Fact] // This should always pass under the same conditions
        public void ValidLicense_ShouldPassValidation()
        {
            // Arrange
            var license = new License(ensureMandatoryKeys: false);
            license["LicenseId"] = "LIC-2026-001";
            license["ExpiryUtc"] = DateTime.UtcNow.AddYears(1);
            license["Signature"] = "ABCD1234SIGNATURE";
            license["CustomerName"] = "Acme Corporation";
            license["MaxUsers"] = 50;
            license["Features"] = new List<string> { "BasicFeature", "PremiumFeature", "EnterpriseFeature" };
            license["IsActive"] = true;
            // Act
            var isValid = _validator.Validate(license, out var errors);
            // Assert
            isValid.ShouldBeTrue();
            errors.ShouldBeEmpty();
        }

        [Fact]
        public void ValidLicense_WithOnlyRequiredFields_ShouldPassValidation()
        {
            // Arrange
            var license = new License(ensureMandatoryKeys: false);
            license["LicenseId"] = "LIC-2026-002";
            license["ExpiryUtc"] = DateTime.UtcNow.AddYears(1);
            license["Signature"] = "SIGNATURE123";
            license["CustomerName"] = "Test Company";
            // Act
            var isValid = _validator.Validate(license, out var errors);
            // Assert
            isValid.ShouldBeTrue();
            errors.ShouldBeEmpty();
        }

        [Fact]
        public void MissingRequiredField_ShouldFailValidation()
        {
            // Arrange
            var license = new License(ensureMandatoryKeys: false);
            license["LicenseId"] = "LIC-2026-003";
            license["ExpiryUtc"] = DateTime.UtcNow.AddYears(1);
            license["Signature"] = "MISSING_CUSTOMER_NAME";
            // CustomerName is missing (required field)
            // Act
            var isValid = _validator.Validate(license, out var errors);
            // Assert
            isValid.ShouldBeFalse();
            errors.ShouldNotBeEmpty();
            errors.Count.ShouldBe(1);
            errors[0].ShouldContain("CustomerName");
            errors[0].ShouldContain("missing or null");
        }

        [Fact]
        public void MissingMultipleRequiredFields_ShouldFailWithAllErrors()
        {
            // Arrange
            var license = new License(ensureMandatoryKeys: false);
            license["LicenseId"] = "LIC-2026-004";
            // Missing ExpiryUtc, Signature, and CustomerName
            // Act
            var isValid = _validator.Validate(license, out var errors);
            // Assert
            isValid.ShouldBeFalse();
            errors.Count.ShouldBe(3);
            errors.ShouldContain(e => e.Contains("ExpiryUtc"));
            errors.ShouldContain(e => e.Contains("Signature"));
            errors.ShouldContain(e => e.Contains("CustomerName"));
        }

        [Fact]
        public void WrongFieldType_String_ShouldFailValidation()
        {
            // Arrange
            var license = new License(ensureMandatoryKeys: false);
            license["LicenseId"] = "LIC-2026-005";
            license["ExpiryUtc"] = DateTime.UtcNow.AddYears(1);
            license["Signature"] = "WRONG_TYPE_TEST";
            license["CustomerName"] = "Test Customer";
            // MaxUsers should be int, but we'll try to set it as string
            // This will fail at field-level validation (FieldValidators)
            Should.Throw<LicenseValidationException>(() =>
            {
                license["MaxUsers"] = "NotANumber";
            });
        }

        [Fact]
        public void WrongFieldType_Bool_ShouldFailValidation()
        {
            // Arrange
            var license = new License(ensureMandatoryKeys: false);
            license["LicenseId"] = "LIC-2026-006";
            license["ExpiryUtc"] = DateTime.UtcNow.AddYears(1);
            license["Signature"] = "TYPE_TEST";
            license["CustomerName"] = "Test Customer";
            // IsActive should be bool - set it directly to bypass field validator
            license.SetField("IsActive", "not a bool", out _);
            // Act
            var isValid = _validator.Validate(license, out var errors);
            // Assert
            isValid.ShouldBeFalse();
            errors.ShouldContain(e => e.Contains("IsActive") && e.Contains("bool"));
        }

        [Fact]
        public void ExtraFieldsNotInSchema_ShouldBeAllowed()
        {
            // Arrange
            var license = new License(ensureMandatoryKeys: false);
            license["LicenseId"] = "LIC-2026-007";
            license["ExpiryUtc"] = DateTime.UtcNow.AddYears(1);
            license["Signature"] = "EXTRA_FIELDS_TEST";
            license["CustomerName"] = "Extra Fields Corp";
            license["MaxUsers"] = 100;
            // Extra fields not in schema (should be allowed)
            license["CompanyAddress"] = "123 Main Street";
            license["PhoneNumber"] = "+1-555-0100";
            license["ExtraField"] = "Some value";
            // Act
            var isValid = _validator.Validate(license, out var errors);
            // Assert
            isValid.ShouldBeTrue();
            errors.ShouldBeEmpty();
        }

        [Fact]
        public void ValidateOrThrow_WithValidLicense_ShouldNotThrow()
        {
            // Arrange
            var license = new License(ensureMandatoryKeys: false);
            license["LicenseId"] = "LIC-2026-008";
            license["ExpiryUtc"] = DateTime.UtcNow.AddYears(1);
            license["Signature"] = "VALID_SIGNATURE";
            license["CustomerName"] = "Valid Company";
            // Act & Assert
            Should.NotThrow(() => _validator.ValidateOrThrow(license));
        }

        [Fact]
        public void ValidateOrThrow_WithInvalidLicense_ShouldThrowWithErrors()
        {
            // Arrange
            var license = new License(ensureMandatoryKeys: false);
            license["LicenseId"] = "LIC-2026-009";
            // Missing required fields
            // Act & Assert
            var exception = Should.Throw<LicenseValidationException>(() =>
            {
                _validator.ValidateOrThrow(license);
            });
            exception.Issues.ShouldNotBeEmpty();
            exception.Issues.Count.ShouldBeGreaterThan(0);
        }

        [Fact]
        public void GetSchemaSummary_ShouldReturnFormattedString()
        {
            // Act
            var summary = _validator.GetSchemaSummary();
            // Assert
            summary.ShouldNotBeNullOrWhiteSpace();
            summary.ShouldContain("DemoSchema");
            summary.ShouldContain("LicenseId");
            summary.ShouldContain("ExpiryUtc");
            summary.ShouldContain("Required");
            summary.ShouldContain("Signed");
        }

        [Fact]
        public void ListField_WithValidElements_ShouldPassValidation()
        {
            // Arrange
            var license = new License(ensureMandatoryKeys: false);
            license["LicenseId"] = "LIC-2026-010";
            license["ExpiryUtc"] = DateTime.UtcNow.AddYears(1);
            license["Signature"] = "LIST_TEST";
            license["CustomerName"] = "List Test Company";
            license["Features"] = new List<string> { "Feature1", "Feature2", "Feature3" };
            // Act
            var isValid = _validator.Validate(license, out var errors);
            // Assert
            isValid.ShouldBeTrue();
            errors.ShouldBeEmpty();
        }

        [Fact]
        public void ListField_WithWrongElementType_ShouldFailValidation()
        {
            // Arrange
            var license = new License(ensureMandatoryKeys: false);
            license["LicenseId"] = "LIC-2026-011";
            license["ExpiryUtc"] = DateTime.UtcNow.AddYears(1);
            license["Signature"] = "LIST_TYPE_TEST";
            license["CustomerName"] = "List Type Test Company";
            // Features should be list<string>, set mixed types directly
            license.SetField("Features", new List<object> { "String", 123, true }, out _);
            // Act
            var isValid = _validator.Validate(license, out var errors);
            // Assert
            isValid.ShouldBeFalse();
            errors.ShouldNotBeEmpty();
            errors.ShouldContain(e => e.Contains("Features"));
        }

        [Fact]
        public void NullValidator_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Should.Throw<ArgumentNullException>(() =>
            {
                var validator = new LicenseValidator(null!);
            });
        }

        [Fact]
        public void ValidateWithNullLicense_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Should.Throw<ArgumentNullException>(() =>
            {
                _validator.Validate(null!, out var errors);
            });
        }

        [Fact]
        public void IntegerField_WithValidInteger_ShouldPassValidation()
        {
            // Arrange
            var license = new License(ensureMandatoryKeys: false);
            license["LicenseId"] = "LIC-2026-012";
            license["ExpiryUtc"] = DateTime.UtcNow.AddYears(1);
            license["Signature"] = "INT_TEST";
            license["CustomerName"] = "Integer Test Company";
            license["MaxUsers"] = 100;
            // Act
            var isValid = _validator.Validate(license, out var errors);
            // Assert
            isValid.ShouldBeTrue();
            errors.ShouldBeEmpty();
        }

        [Fact]
        public void DateTimeField_WithValidDateTime_ShouldPassValidation()
        {
            // Arrange
            var license = new License(ensureMandatoryKeys: false);
            license["LicenseId"] = "LIC-2026-013";
            license["ExpiryUtc"] = DateTime.UtcNow.AddYears(1);
            license["Signature"] = "DATETIME_TEST";
            license["CustomerName"] = "DateTime Test Company";
            // Act
            var isValid = _validator.Validate(license, out var errors);
            // Assert
            isValid.ShouldBeTrue();
            errors.ShouldBeEmpty();
            license["ExpiryUtc"].ShouldBeOfType<DateTime>();
        }

        [Fact]
        public void DateTimeField_WithStringValue_ShouldPassIfParseable()
        {
            // Arrange
            var license = new License(ensureMandatoryKeys: false);
            license["LicenseId"] = "LIC-2026-014";
            license["ExpiryUtc"] = "2027-12-31"; // String that can be parsed
            license["Signature"] = "STRING_DATE_TEST";
            license["CustomerName"] = "String Date Test Company";
            // Act
            var isValid = _validator.Validate(license, out var errors);
            // Assert
            isValid.ShouldBeTrue();
            errors.ShouldBeEmpty();
            // Field validator should have converted string to DateTime
            license["ExpiryUtc"].ShouldBeOfType<DateTime>();
        }

        [Fact]
        public void BooleanField_WithValidBoolean_ShouldPassValidation()
        {
            // Arrange
            var license = new License(ensureMandatoryKeys: false);
            license["LicenseId"] = "LIC-2026-015";
            license["ExpiryUtc"] = DateTime.UtcNow.AddYears(1);
            license["Signature"] = "BOOL_TEST";
            license["CustomerName"] = "Boolean Test Company";
            license["IsActive"] = true;
            // Act
            var isValid = _validator.Validate(license, out var errors);
            // Assert
            isValid.ShouldBeTrue();
            errors.ShouldBeEmpty();
        }
    }
}

