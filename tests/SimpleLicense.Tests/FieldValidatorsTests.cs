using System.Text.Json;
using Xunit;
using Shouldly;

using SimpleLicense.Core.LicenseValidation;

namespace SimpleLicense.Tests
{
    /// <summary>
    /// Tests for FieldValidators - Field-level validation registry and validators
    /// </summary>
    public class FieldValidatorsTests
    {
        [Fact]
        public void All_ShouldReturnDefaultValidators()
        {
            // Act
            var validators = FieldValidators.All;

            // Assert
            validators.ShouldContainKey("LicenseId");
            validators.ShouldContainKey("ExpiryUtc");
            validators.ShouldContainKey("Signature");
        }

        [Fact]
        public void All_ShouldIncludeAutoDiscoveredValidators()
        {
            // Act
            var validators = FieldValidators.All;

            // Assert
            validators.ShouldContainKey("MaxUsers");
            validators.ShouldContainKey("CustomerName");
        }

        [Fact]
        public void Register_ShouldAddValidatorToRegistry()
        {
            // Arrange
            FieldValidator customValidator = (value) => ValidationResult.Success(value);

            // Act
            FieldValidators.Register("CustomField", customValidator);

            // Assert
            FieldValidators.TryGetValidator("CustomField", out var validator).ShouldBeTrue();
            validator.ShouldNotBeNull();
        }

        [Fact]
        public void Register_WithNullFieldName_ShouldThrow()
        {
            // Arrange
            FieldValidator validator = (value) => ValidationResult.Success(value);

            // Act & Assert
            Should.Throw<ArgumentNullException>(() => FieldValidators.Register(null!, validator));
        }

        [Fact]
        public void Register_WithNullValidator_ShouldThrow()
        {
            // Act & Assert
            Should.Throw<ArgumentNullException>(() => FieldValidators.Register("Field", null!));
        }

        [Fact]
        public void TryGetValidator_WithExistingValidator_ShouldReturnTrue()
        {
            // Act
            var result = FieldValidators.TryGetValidator("LicenseId", out var validator);

            // Assert
            result.ShouldBeTrue();
            validator.ShouldNotBeNull();
        }

        [Fact]
        public void TryGetValidator_WithNonExistentValidator_ShouldReturnFalse()
        {
            // Act
            var result = FieldValidators.TryGetValidator("NonExistentField", out var validator);

            // Assert
            result.ShouldBeFalse();
            validator.ShouldBeNull();
        }

        [Fact]
        public void GetValidator_WithExistingValidator_ShouldReturnValidator()
        {
            // Act
            var validator = FieldValidators.GetValidator("LicenseId");

            // Assert
            validator.ShouldNotBeNull();
        }

        [Fact]
        public void GetValidator_WithNonExistentValidator_ShouldReturnNull()
        {
            // Act
            var validator = FieldValidators.GetValidator("NonExistentField");

            // Assert
            validator.ShouldBeNull();
        }

        // ============================================================================
        // ValidateLicenseId Tests
        // ============================================================================

        [Fact]
        public void ValidateLicenseId_WithValidString_ShouldSucceed()
        {
            // Act
            var result = FieldValidators.ValidateLicenseId("VALID-LICENSE-ID");

            // Assert
            result.IsValid.ShouldBeTrue();
            result.OutputValue.ShouldBe("VALID-LICENSE-ID");
        }

        [Fact]
        public void ValidateLicenseId_WithWhitespace_ShouldTrim()
        {
            // Act
            var result = FieldValidators.ValidateLicenseId("  TRIMMED-ID  ");

            // Assert
            result.IsValid.ShouldBeTrue();
            result.OutputValue.ShouldBe("TRIMMED-ID");
        }

        [Fact]
        public void ValidateLicenseId_WithEmptyString_ShouldFail()
        {
            // Act
            var result = FieldValidators.ValidateLicenseId("");

            // Assert
            result.IsValid.ShouldBeFalse();
            result.Error!.ShouldContain("empty");
        }

        [Fact]
        public void ValidateLicenseId_WithWhitespaceOnly_ShouldFail()
        {
            // Act
            var result = FieldValidators.ValidateLicenseId("   ");

            // Assert
            result.IsValid.ShouldBeFalse();
            result.Error!.ShouldContain("empty");
        }

        [Fact]
        public void ValidateLicenseId_WithNull_ShouldFail()
        {
            // Act
            var result = FieldValidators.ValidateLicenseId(null);

            // Assert
            result.IsValid.ShouldBeFalse();
            result.Error!.ShouldContain("required");
        }

        [Fact]
        public void ValidateLicenseId_WithNonString_ShouldFail()
        {
            // Act
            var result = FieldValidators.ValidateLicenseId(123);

            // Assert
            result.IsValid.ShouldBeFalse();
            result.Error!.ShouldContain("string");
        }

        [Fact]
        public void ValidateLicenseId_WithJsonElement_ShouldHandleCorrectly()
        {
            // Arrange
            var json = JsonDocument.Parse(@"{""id"": ""JSON-ID-123""}");
            var element = json.RootElement.GetProperty("id");

            // Act
            var result = FieldValidators.ValidateLicenseId(element);

            // Assert
            result.IsValid.ShouldBeTrue();
            result.OutputValue.ShouldBe("JSON-ID-123");
        }

        // ============================================================================
        // ValidateExpiryUtc Tests
        // ============================================================================

        [Fact]
        public void ValidateExpiryUtc_WithDateTime_ShouldSucceed()
        {
            // Arrange
            var dt = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc);

            // Act
            var result = FieldValidators.ValidateExpiryUtc(dt);

            // Assert
            result.IsValid.ShouldBeTrue();
            result.OutputValue.ShouldBeOfType<DateTime>();
            ((DateTime)result.OutputValue!).Kind.ShouldBe(DateTimeKind.Utc);
        }

        [Fact]
        public void ValidateExpiryUtc_WithLocalDateTime_ShouldConvertToUtc()
        {
            // Arrange
            var dt = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Local);

            // Act
            var result = FieldValidators.ValidateExpiryUtc(dt);

            // Assert
            result.IsValid.ShouldBeTrue();
            ((DateTime)result.OutputValue!).Kind.ShouldBe(DateTimeKind.Utc);
        }

        [Fact]
        public void ValidateExpiryUtc_WithDateTimeOffset_ShouldConvertToUtc()
        {
            // Arrange
            var dto = new DateTimeOffset(2025, 12, 31, 23, 59, 59, TimeSpan.FromHours(-5));

            // Act
            var result = FieldValidators.ValidateExpiryUtc(dto);

            // Assert
            result.IsValid.ShouldBeTrue();
            result.OutputValue.ShouldBeOfType<DateTime>();
            ((DateTime)result.OutputValue!).Kind.ShouldBe(DateTimeKind.Utc);
        }

        [Fact]
        public void ValidateExpiryUtc_WithYear_ShouldCreateFirstDayOfYear()
        {
            // Act
            var result = FieldValidators.ValidateExpiryUtc(2025);

            // Assert
            result.IsValid.ShouldBeTrue();
            var dt = (DateTime)result.OutputValue!;
            dt.Year.ShouldBe(2025);
            dt.Month.ShouldBe(1);
            dt.Day.ShouldBe(1);
        }

        [Fact]
        public void ValidateExpiryUtc_WithUnixTimestampSeconds_ShouldConvert()
        {
            // Arrange - Unix timestamp for 2025-12-31 23:59:59 UTC
            var unixSeconds = 1767225599L;

            // Act
            var result = FieldValidators.ValidateExpiryUtc(unixSeconds);

            // Assert
            result.IsValid.ShouldBeTrue();
            var dt = (DateTime)result.OutputValue!;
            dt.Year.ShouldBe(2025);
            dt.Month.ShouldBe(12);
        }

        [Fact]
        public void ValidateExpiryUtc_WithUnixTimestampMilliseconds_ShouldConvert()
        {
            // Arrange - Unix timestamp in milliseconds
            var unixMs = 1767225599000L;

            // Act
            var result = FieldValidators.ValidateExpiryUtc(unixMs);

            // Assert
            result.IsValid.ShouldBeTrue();
            var dt = (DateTime)result.OutputValue!;
            dt.Year.ShouldBe(2025);
        }

        [Fact]
        public void ValidateExpiryUtc_WithIsoString_ShouldParse()
        {
            // Act
            var result = FieldValidators.ValidateExpiryUtc("2025-12-31T23:59:59Z");

            // Assert
            result.IsValid.ShouldBeTrue();
            var dt = (DateTime)result.OutputValue!;
            dt.Year.ShouldBe(2025);
            dt.Month.ShouldBe(12);
            dt.Day.ShouldBe(31);
        }

        [Fact]
        public void ValidateExpiryUtc_WithDateOnlyString_ShouldParse()
        {
            // Act
            var result = FieldValidators.ValidateExpiryUtc("2025-12-31");

            // Assert
            result.IsValid.ShouldBeTrue();
            var dt = (DateTime)result.OutputValue!;
            dt.Year.ShouldBe(2025);
            dt.Month.ShouldBe(12);
            dt.Day.ShouldBe(31);
        }

        [Fact]
        public void ValidateExpiryUtc_WithSlashDateFormat_ShouldParse()
        {
            // Act
            var result = FieldValidators.ValidateExpiryUtc("12/31/2025");

            // Assert
            result.IsValid.ShouldBeTrue();
            var dt = (DateTime)result.OutputValue!;
            dt.Year.ShouldBe(2025);
        }

        [Fact]
        public void ValidateExpiryUtc_WithVariousFormats_ShouldParseAll()
        {
            // Arrange
            var formats = new[]
            {
                "2025-12-31T10:30:00",
                "2025-12-31 10:30:00",
                "12/31/2025 10:30:00",
                "31/12/2025",
                "2025-12-31"
            };

            foreach (var format in formats)
            {
                // Act
                var result = FieldValidators.ValidateExpiryUtc(format);

                // Assert
                result.IsValid.ShouldBeTrue($"Format '{format}' should parse");
                ((DateTime)result.OutputValue!).Year.ShouldBe(2025);
            }
        }

        [Fact]
        public void ValidateExpiryUtc_WithNull_ShouldFail()
        {
            // Act
            var result = FieldValidators.ValidateExpiryUtc(null);

            // Assert
            result.IsValid.ShouldBeFalse();
            result.Error!.ShouldContain("required");
        }

        [Fact]
        public void ValidateExpiryUtc_WithInvalidString_ShouldFail()
        {
            // Act
            var result = FieldValidators.ValidateExpiryUtc("not-a-date");

            // Assert
            result.IsValid.ShouldBeFalse();
            result.Error!.ShouldContain("parsed");
        }

        [Fact]
        public void ValidateExpiryUtc_WithJsonElement_ShouldHandleStringAndNumeric()
        {
            // Arrange - String
            var jsonString = JsonDocument.Parse(@"{""exp"": ""2025-12-31T23:59:59Z""}");
            var stringElement = jsonString.RootElement.GetProperty("exp");

            // Act
            var result1 = FieldValidators.ValidateExpiryUtc(stringElement);

            // Assert
            result1.IsValid.ShouldBeTrue();

            // Arrange - Numeric
            var jsonNumeric = JsonDocument.Parse(@"{""exp"": 1767225599}");
            var numericElement = jsonNumeric.RootElement.GetProperty("exp");

            // Act
            var result2 = FieldValidators.ValidateExpiryUtc(numericElement);

            // Assert
            result2.IsValid.ShouldBeTrue();
        }

        // ============================================================================
        // ValidateSignature Tests
        // ============================================================================

        [Fact]
        public void ValidateSignature_WithValidString_ShouldSucceed()
        {
            // Act
            var result = FieldValidators.ValidateSignature("valid-signature-data");

            // Assert
            result.IsValid.ShouldBeTrue();
            result.OutputValue.ShouldBe("valid-signature-data");
        }

        [Fact]
        public void ValidateSignature_WithNull_ShouldSucceed()
        {
            // Act - Signature can be null for unsigned licenses
            var result = FieldValidators.ValidateSignature(null);

            // Assert
            result.IsValid.ShouldBeTrue();
            result.OutputValue.ShouldBeNull();
        }

        [Fact]
        public void ValidateSignature_WithEmptyString_ShouldFail()
        {
            // Act
            var result = FieldValidators.ValidateSignature("");

            // Assert
            result.IsValid.ShouldBeFalse();
            result.Error!.ShouldContain("empty");
        }

        [Fact]
        public void ValidateSignature_WithWhitespaceOnly_ShouldFail()
        {
            // Act
            var result = FieldValidators.ValidateSignature("   ");

            // Assert
            result.IsValid.ShouldBeFalse();
            result.Error!.ShouldContain("empty");
        }

        [Fact]
        public void ValidateSignature_WithNonString_ShouldFail()
        {
            // Act
            var result = FieldValidators.ValidateSignature(123);

            // Assert
            result.IsValid.ShouldBeFalse();
            result.Error!.ShouldContain("string");
        }

        [Fact]
        public void ValidateSignature_WithJsonElement_ShouldHandleStringAndNull()
        {
            // Arrange - String
            var jsonString = JsonDocument.Parse(@"{""sig"": ""signature-data""}");
            var stringElement = jsonString.RootElement.GetProperty("sig");

            // Act
            var result1 = FieldValidators.ValidateSignature(stringElement);

            // Assert
            result1.IsValid.ShouldBeTrue();
            result1.OutputValue.ShouldBe("signature-data");

            // Arrange - Null
            var jsonNull = JsonDocument.Parse(@"{""sig"": null}");
            var nullElement = jsonNull.RootElement.GetProperty("sig");

            // Act
            var result2 = FieldValidators.ValidateSignature(nullElement);

            // Assert
            result2.IsValid.ShouldBeTrue();
            result2.OutputValue.ShouldBeNull();
        }

        // ============================================================================
        // ValidateMaxUsers Tests (Custom Validator)
        // ============================================================================

        [Fact]
        public void ValidateMaxUsers_WithValidPositiveInteger_ShouldSucceed()
        {
            // Act
            var result = FieldValidators.ValidateMaxUsers(10);

            // Assert
            result.IsValid.ShouldBeTrue();
            result.OutputValue.ShouldBe(10);
        }

        [Fact]
        public void ValidateMaxUsers_WithZero_ShouldSucceed()
        {
            // Act
            var result = FieldValidators.ValidateMaxUsers(0);

            // Assert
            result.IsValid.ShouldBeTrue();
            result.OutputValue.ShouldBe(0);
        }

        [Fact]
        public void ValidateMaxUsers_WithNull_ShouldSucceed()
        {
            // Act - Optional field
            var result = FieldValidators.ValidateMaxUsers(null);

            // Assert
            result.IsValid.ShouldBeTrue();
            result.OutputValue.ShouldBeNull();
        }

        [Fact]
        public void ValidateMaxUsers_WithNegative_ShouldFail()
        {
            // Act
            var result = FieldValidators.ValidateMaxUsers(-5);

            // Assert
            result.IsValid.ShouldBeFalse();
            result.Error!.ShouldContain("non-negative");
        }

        [Fact]
        public void ValidateMaxUsers_WithDecimal_ShouldFail()
        {
            // Act
            var result = FieldValidators.ValidateMaxUsers(10.5);

            // Assert
            result.IsValid.ShouldBeFalse();
            result.Error!.ShouldContain("integer");
        }

        [Fact]
        public void ValidateMaxUsers_WithNonNumeric_ShouldFail()
        {
            // Act
            var result = FieldValidators.ValidateMaxUsers("not-a-number");

            // Assert
            result.IsValid.ShouldBeFalse();
            result.Error!.ShouldContain("number");
        }

        [Fact]
        public void ValidateMaxUsers_WithDouble_ShouldConvertToInt()
        {
            // Act
            var result = FieldValidators.ValidateMaxUsers(100.0);

            // Assert
            result.IsValid.ShouldBeTrue();
            result.OutputValue.ShouldBe(100);
        }

        // ============================================================================
        // ValidateCustomerName Tests (Custom Validator)
        // ============================================================================

        [Fact]
        public void ValidateCustomerName_WithValidString_ShouldSucceed()
        {
            // Act
            var result = FieldValidators.ValidateCustomerName("John Doe");

            // Assert
            result.IsValid.ShouldBeTrue();
            result.OutputValue.ShouldBe("John Doe");
        }

        [Fact]
        public void ValidateCustomerName_WithWhitespace_ShouldTrim()
        {
            // Act
            var result = FieldValidators.ValidateCustomerName("  Trimmed Name  ");

            // Assert
            result.IsValid.ShouldBeTrue();
            result.OutputValue.ShouldBe("Trimmed Name");
        }

        [Fact]
        public void ValidateCustomerName_WithNull_ShouldSucceed()
        {
            // Act - Optional unless schema says required
            var result = FieldValidators.ValidateCustomerName(null);

            // Assert
            result.IsValid.ShouldBeTrue();
            result.OutputValue.ShouldBeNull();
        }

        [Fact]
        public void ValidateCustomerName_WithEmptyString_ShouldFail()
        {
            // Act
            var result = FieldValidators.ValidateCustomerName("");

            // Assert
            result.IsValid.ShouldBeFalse();
            result.Error!.ShouldContain("empty");
        }

        [Fact]
        public void ValidateCustomerName_WithWhitespaceOnly_ShouldFail()
        {
            // Act
            var result = FieldValidators.ValidateCustomerName("   ");

            // Assert
            result.IsValid.ShouldBeFalse();
            result.Error!.ShouldContain("empty");
        }

        [Fact]
        public void ValidateCustomerName_WithNonString_ShouldFail()
        {
            // Act
            var result = FieldValidators.ValidateCustomerName(123);

            // Assert
            result.IsValid.ShouldBeFalse();
            result.Error!.ShouldContain("string");
        }

        // ============================================================================
        // Integration Tests
        // ============================================================================

        [Fact]
        public void Validators_ShouldBeCaseInsensitive()
        {
            // Act
            var result1 = FieldValidators.TryGetValidator("LicenseId", out _);
            var result2 = FieldValidators.TryGetValidator("licenseid", out _);
            var result3 = FieldValidators.TryGetValidator("LICENSEID", out _);

            // Assert
            result1.ShouldBeTrue();
            result2.ShouldBeTrue();
            result3.ShouldBeTrue();
        }

        [Fact]
        public void Register_ShouldOverrideExistingValidator()
        {
            // Arrange
            var originalCalled = false;
            var overrideCalled = false;
            
            FieldValidator originalValidator = (value) =>
            {
                originalCalled = true;
                return ValidationResult.Success(value);
            };
            
            FieldValidator overrideValidator = (value) =>
            {
                overrideCalled = true;
                return ValidationResult.Success(value);
            };

            // Act
            FieldValidators.Register("OverrideTest", originalValidator);
            FieldValidators.Register("OverrideTest", overrideValidator);
            
            var validator = FieldValidators.GetValidator("OverrideTest");
            validator!("test");

            // Assert
            overrideCalled.ShouldBeTrue();
            originalCalled.ShouldBeFalse();
        }

        [Fact]
        public void AutoDiscovery_ShouldRegisterAttributedValidators()
        {
            // Act - Access All to trigger initialization
            var validators = FieldValidators.All;

            // Assert - Validators with [FieldValidator] attribute should be registered
            validators.ShouldContainKey("MaxUsers");
            validators.ShouldContainKey("CustomerName");
        }

        [Fact]
        public void ValidateExpiryUtc_WithEdgeCaseYears_ShouldHandle()
        {
            // Act & Assert - Year 1
            var result1 = FieldValidators.ValidateExpiryUtc(1);
            result1.IsValid.ShouldBeTrue();
            ((DateTime)result1.OutputValue!).Year.ShouldBe(1);

            // Act & Assert - Year 9999
            var result2 = FieldValidators.ValidateExpiryUtc(9999);
            result2.IsValid.ShouldBeTrue();
            ((DateTime)result2.OutputValue!).Year.ShouldBe(9999);
        }

        [Fact]
        public void ValidateExpiryUtc_WithSmallNumeric_ShouldInterpretAsUnixTimestamp()
        {
            // Act - Number that's too large to be a year will be interpreted as unix timestamp
            var result = FieldValidators.ValidateExpiryUtc(12345);

            // Assert - 12345 is a valid unix timestamp (Jan 1, 1970 + 12345 seconds)
            result.IsValid.ShouldBeTrue();
            var dt = (DateTime)result.OutputValue!;
            dt.Year.ShouldBe(1970);
        }

        [Fact]
        public void FieldValidatorAttribute_ShouldStoreFieldName()
        {
            // Arrange
            var attr = new FieldValidatorAttribute("TestField");

            // Assert
            attr.FieldName.ShouldBe("TestField");
        }

        [Fact]
        public async Task ThreadSafety_MultipleRegistrations_ShouldHandleCorrectly()
        {
            // Arrange
            var tasks = new List<Task>();
            
            // Act - Register validators from multiple threads
            for (int i = 0; i < 10; i++)
            {
                var index = i;
                tasks.Add(Task.Run(() =>
                {
                    FieldValidators.Register($"ThreadTest{index}", (value) => ValidationResult.Success(value));
                }));
            }

            await Task.WhenAll(tasks);

            // Assert - All validators should be registered
            for (int i = 0; i < 10; i++)
            {
                FieldValidators.TryGetValidator($"ThreadTest{i}", out var validator).ShouldBeTrue();
                validator.ShouldNotBeNull();
            }
        }
    }
}
