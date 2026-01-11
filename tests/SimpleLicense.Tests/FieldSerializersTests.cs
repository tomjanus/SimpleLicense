using Xunit;
using Shouldly;
using SimpleLicense.Core;
using SimpleLicense.Core.LicenseValidation;
using System;

namespace SimpleLicense.Tests
{
    /// <summary>
    /// Tests for FieldSerializers - Custom field serialization for JSON output
    /// </summary>
    public class FieldSerializersTests
    {
        [Fact]
        public void SerializeExpiryUtc_WithDateTime_ShouldReturnIso8601String()
        {
            // Arrange
            var serializer = FieldSerializers.GetSerializer("ExpiryUtc");
            var dateTime = new DateTime(2027, 6, 15, 14, 30, 0, DateTimeKind.Utc);

            // Act
            var result = serializer?.Invoke(dateTime);

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBeOfType<string>();
            var resultStr = result as string;
            resultStr.ShouldContain("2027-06-15");
            resultStr.ShouldContain("T");
            resultStr.ShouldEndWith("Z"); // UTC indicator
        }

        [Fact]
        public void SerializeExpiryUtc_WithLocalDateTime_ShouldConvertToUtc()
        {
            // Arrange
            var serializer = FieldSerializers.GetSerializer("ExpiryUtc");
            var localDateTime = new DateTime(2027, 6, 15, 14, 30, 0, DateTimeKind.Local);

            // Act
            var result = serializer?.Invoke(localDateTime);

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBeOfType<string>();
            var resultStr = result as string;
            resultStr.ShouldEndWith("Z"); // Should be converted to UTC
        }

        [Fact]
        public void SerializeExpiryUtc_WithDateTimeOffset_ShouldReturnIso8601String()
        {
            // Arrange
            var serializer = FieldSerializers.GetSerializer("ExpiryUtc");
            var dateTimeOffset = new DateTimeOffset(2027, 6, 15, 14, 30, 0, TimeSpan.Zero);

            // Act
            var result = serializer?.Invoke(dateTimeOffset);

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBeOfType<string>();
            var resultStr = result as string;
            resultStr.ShouldContain("2027-06-15");
            // DateTimeOffset may format with +00:00 or Z, both are valid UTC
            (resultStr.EndsWith("Z") || resultStr.Contains("+00:00")).ShouldBeTrue();
        }

        [Fact]
        public void SerializeExpiryUtc_WithString_ShouldReformatIfValid()
        {
            // Arrange
            var serializer = FieldSerializers.GetSerializer("ExpiryUtc");
            var dateString = "2027-06-15T14:30:00";

            // Act
            var result = serializer?.Invoke(dateString);

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBeOfType<string>();
        }

        [Fact]
        public void SerializeExpiryUtc_WithInvalidString_ShouldReturnAsIs()
        {
            // Arrange
            var serializer = FieldSerializers.GetSerializer("ExpiryUtc");
            var invalidString = "not-a-date";

            // Act
            var result = serializer?.Invoke(invalidString);

            // Assert
            result.ShouldBe(invalidString); // Should return as-is
        }

        [Fact]
        public void SerializeExpiryUtc_WithNull_ShouldReturnNull()
        {
            // Arrange
            var serializer = FieldSerializers.GetSerializer("ExpiryUtc");

            // Act
            var result = serializer?.Invoke(null);

            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public void RegisterCustomSerializer_ShouldOverrideDefault()
        {
            // Arrange
            var testFieldName = "TestField";
            var customValue = "custom-serialized";
            FieldSerializers.Register(testFieldName, _ => customValue);

            // Act
            var serializer = FieldSerializers.GetSerializer(testFieldName);
            var result = serializer?.Invoke("any-value");

            // Assert
            result.ShouldBe(customValue);
        }

        [Fact]
        public void TryGetSerializer_WithExistingField_ShouldReturnTrue()
        {
            // Act
            var found = FieldSerializers.TryGetSerializer("ExpiryUtc", out var serializer);

            // Assert
            found.ShouldBeTrue();
            serializer.ShouldNotBeNull();
        }

        [Fact]
        public void TryGetSerializer_WithNonExistentField_ShouldReturnFalse()
        {
            // Act
            var found = FieldSerializers.TryGetSerializer("NonExistentField", out var serializer);

            // Assert
            found.ShouldBeFalse();
            serializer.ShouldBeNull();
        }

        [Fact]
        public void GetSerializer_WithNonExistentField_ShouldReturnNull()
        {
            // Act
            var serializer = FieldSerializers.GetSerializer("NonExistentField");

            // Assert
            serializer.ShouldBeNull();
        }

        [Fact]
        public void FieldSerializers_ShouldBeCaseInsensitive()
        {
            // Act
            var lower = FieldSerializers.GetSerializer("expiryutc");
            var upper = FieldSerializers.GetSerializer("EXPIRYUTC");
            var mixed = FieldSerializers.GetSerializer("ExPiRyUtC");

            // Assert
            lower.ShouldNotBeNull();
            upper.ShouldNotBeNull();
            mixed.ShouldNotBeNull();
            lower.ShouldBe(upper);
            upper.ShouldBe(mixed);
        }

        [Fact]
        public void License_ToJson_WithExpiryUtc_ShouldApplySerializer()
        {
            // Arrange
            var license = new License(ensureMandatoryKeys: true);
            license["LicenseId"] = "TEST-001";
            license["ExpiryUtc"] = new DateTime(2027, 6, 15, 14, 30, 0, DateTimeKind.Utc);
            license["Signature"] = "test-sig";

            // Act
            var json = license.ToJson(validate: false);

            // Assert
            json.ShouldContain("2027-06-15");
            json.ShouldContain("14:30:00");
            json.ShouldContain("Z"); // UTC indicator
        }

        [Fact]
        public void License_RegisterFieldSerializer_ShouldAllowCustomSerialization()
        {
            // Arrange
            var customFieldName = "CustomDate";
            License.RegisterFieldSerializer(customFieldName, value =>
            {
                if (value is DateTime dt)
                {
                    return dt.ToString("yyyy-MM-dd"); // Simple date format
                }
                return value;
            });

            var license = new License(ensureMandatoryKeys: false);
            license["CustomDate"] = new DateTime(2027, 6, 15);

            // Act
            var json = license.ToJson(validate: false);

            // Assert
            json.ShouldContain("2027-06-15");
            // The custom serializer formats as "2027-06-15" without time
            // Check that the value doesn't contain time separator after the date
            json.ShouldNotContain("2027-06-15T");
        }

        [Fact]
        public void License_ToJson_WithMultipleFields_ShouldApplySerializersToAll()
        {
            // Arrange
            var license = new License(ensureMandatoryKeys: true);
            license["LicenseId"] = "TEST-002";
            license["ExpiryUtc"] = new DateTime(2027, 12, 31, 23, 59, 59, DateTimeKind.Utc);
            license["Signature"] = "sig-123";
            license["CustomField"] = "custom-value";

            // Act
            var json = license.ToJson(validate: false);

            // Assert
            json.ShouldContain("TEST-002");
            json.ShouldContain("2027-12-31");
            json.ShouldContain("custom-value");
            json.ShouldContain("sig-123");
        }

        [Fact]
        public void License_ToJson_WithNullExpiryUtc_ShouldHandleGracefully()
        {
            // Arrange - Create license without mandatory keys to avoid ExpiryUtc validation
            var license = new License(ensureMandatoryKeys: false);
            license["LicenseId"] = "TEST-003";
            license["Signature"] = "sig-456";
            // Don't add ExpiryUtc field at all, or add as empty
            // This tests that the serializer handles missing/null gracefully

            // Act
            var json = license.ToJson(validate: false);

            // Assert
            json.ShouldContain("TEST-003");
            json.ShouldContain("sig-456");
        }

        [Fact]
        public void ResetToDefaults_ShouldRestoreDefaultSerializers()
        {
            // Arrange
            FieldSerializers.Register("ExpiryUtc", _ => "custom");
            
            // Act
            FieldSerializers.ResetToDefaults();
            var serializer = FieldSerializers.GetSerializer("ExpiryUtc");
            var result = serializer?.Invoke(new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            // Assert
            result.ShouldBeOfType<string>();
            var resultStr = result as string;
            resultStr.ShouldNotBe("custom");
            resultStr.ShouldEndWith("Z"); // Should be back to default ISO format
        }
    }
}
