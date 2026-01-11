# SimpleLicense.Tests

Comprehensive test suite for the SimpleLicense library, providing full coverage of license creation, signing, validation, serialization, and file protection features.

## Overview

This test suite uses **xUnit**, **Shouldly**, and **Moq** to ensure the reliability and correctness of SimpleLicense's core functionality. Tests are organized by feature area and follow a clear Arrange-Act-Assert pattern.

## Test Framework

- **xUnit 2.9.3** - Primary testing framework
- **Shouldly 4.3.0** - Fluent assertion library for readable test expectations
- **Moq 4.20.70** - Mocking framework for dependencies
- **Microsoft.NET.Test.Sdk 17.11.1** - Test execution platform

## Running Tests

### Run All Tests

```bash
# From solution root
dotnet test

# From test project directory
cd tests/SimpleLicense.Tests
dotnet test

# With verbose output
dotnet test --logger "console;verbosity=detailed"

# With code coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Run Specific Test Classes

```bash
# Run a specific test class
dotnet test --filter "FullyQualifiedName~LicenseSigningAndVerificationTests"

# Run tests matching a pattern
dotnet test --filter "FullyQualifiedName~Validation"
```

### Run Individual Tests

```bash
# Run a specific test method
dotnet test --filter "FullyQualifiedName~LicenseSigningAndVerificationTests.SignLicenseDocument_AddsSignatureToLicense"
```

## Test Coverage

### Core License Functionality

#### [LicenseDocumentTests.cs](LicenseDocumentTests.cs)
Tests the fundamental `License` class that represents license documents:
- License creation and field management
- Field type validation (string, int, double, bool, datetime, lists)
- Schema compliance validation
- Field access patterns (dictionary-style and property-style)
- Edge cases and error handling

#### [LicenseSchemaTests.cs](LicenseSchemaTests.cs)
Tests the `LicenseSchema` class that defines license structure:
- Schema creation and validation
- Field descriptor definitions (type, required, default values)
- Schema serialization to/from YAML and JSON
- Schema validation rules
- Default value handling

### Cryptography & Security

#### [LicenseSigningAndVerificationTests.cs](LicenseSigningAndVerificationTests.cs)
Tests digital signature creation and verification (582 lines):
- RSA signature generation with PSS and PKCS1 padding
- License signing with private keys
- Signature verification with public keys
- Tamper detection (modified licenses fail verification)
- Different key sizes (2048, 3072, 4096 bits)
- Canonical serialization before signing
- Error handling for invalid keys and corrupted signatures

#### [HashingTests.cs](HashingTests.cs)
Tests SHA-256 hashing functionality:
- File content hashing
- String content hashing
- Hash consistency and determinism
- Hash format validation
- Error handling for missing files

### Validation

#### [LicenseValidationTests.cs](LicenseValidationTests.cs)
Tests schema-level license validation (332 lines):
- Required field validation
- Field type validation
- Default value application
- Invalid field detection
- Missing field detection
- Schema compliance validation
- Comprehensive error reporting

#### [FieldValidatorsTests.cs](FieldValidatorsTests.cs)
Tests field-level validators:
- `NotExpiredValidator` - Ensures datetime fields haven't passed
- `RegexValidator` - Pattern matching for string fields
- `RangeValidator` - Numeric range validation
- Custom validator implementations
- Validator chaining and composition
- Error message generation

### Serialization

#### [LicenseSerializerTests.cs](LicenseSerializerTests.cs)
Tests license serialization to/from JSON and YAML:
- JSON serialization/deserialization
- YAML serialization/deserialization
- Canonical serialization (deterministic output)
- Type preservation (datetime, lists, etc.)
- Schema attachment to serialized licenses
- Round-trip consistency

#### [FieldSerializersTests.cs](FieldSerializersTests.cs)
Tests custom field serialization:
- `Base64Serializer` - Binary data encoding
- `UppercaseSerializer` - String transformation
- Custom serializer implementations
- Serializer composition and chaining

### Field Processing

#### [FieldProcessorsTests.cs](FieldProcessorsTests.cs)
Tests automated field processors:
- `GenerateGuidProcessor` - Auto-generate unique license IDs
- `CurrentTimestampProcessor` - Add creation timestamps
- `HashFileProcessor` - Generate file hashes
- `HashFilesProcessor` - Generate multiple file hashes
- `ToUpperProcessor` / `ToLowerProcessor` - String transformations
- Processor execution order and dependencies
- Error handling in processors

### File Protection & Canonicalization

#### [GenericTextFileCanonalizerTests.cs](GenericTextFileCanonalizerTests.cs)
Tests generic text file canonicalization:
- Comment removal (single-line and multi-line)
- Whitespace normalization
- Blank line removal
- Multiple comment style support (# // /* */ etc.)
- YAML, JSON, CSV, and config file handling
- Deterministic output for consistent hashing

#### [InpFileCanonalizerTests.cs](InpFileCanonalizerTests.cs)
Tests EPANET .inp file canonicalization:
- INP format-specific parsing
- Section-aware canonicalization
- Comment and whitespace handling
- Preserving semantic meaning while normalizing format
- Consistent hashing of hydraulic network files

## Test Patterns & Best Practices

### Arrange-Act-Assert Pattern

All tests follow the clear AAA pattern:

```csharp
[Fact]
public void MyTest_Scenario_ExpectedBehavior()
{
    // Arrange - Set up test data and dependencies
    var license = CreateTestLicense();
    var validator = new LicenseValidator(schema);
    
    // Act - Execute the operation being tested
    var result = validator.Validate(license, out var errors);
    
    // Assert - Verify expected outcomes
    result.ShouldBeTrue();
    errors.ShouldBeEmpty();
}
```

### Shouldly Assertions

Tests use Shouldly's fluent assertions for readability:

```csharp
result.ShouldBeTrue();
value.ShouldBe(expected);
collection.ShouldBeEmpty();
exception.ShouldNotBeNull();
message.ShouldContain("expected text");
```

### Test Naming Convention

Test methods follow the pattern: `MethodName_Scenario_ExpectedBehavior`

Examples:
- `SignLicenseDocument_AddsSignatureToLicense`
- `ValidateLicense_WithMissingRequiredField_ShouldFail`
- `DeserializeLicense_FromJson_ShouldPreserveTypes`

### Test Data Management

- **In-memory keys**: RSA keys generated in test constructors
- **Test schemas**: Defined in test class constructors for reuse
- **Helper methods**: `CreateTestLicense()` methods for common test data
- **Isolation**: Each test creates its own data, no shared state

## Test Organization

```
SimpleLicense.Tests/
├── Core License
│   ├── LicenseDocumentTests.cs
│   └── LicenseSchemaTests.cs
├── Security
│   ├── LicenseSigningAndVerificationTests.cs
│   └── HashingTests.cs
├── Validation
│   ├── LicenseValidationTests.cs
│   └── FieldValidatorsTests.cs
├── Serialization
│   ├── LicenseSerializerTests.cs
│   └── FieldSerializersTests.cs
├── Processing
│   └── FieldProcessorsTests.cs
└── Canonicalization
    ├── GenericTextFileCanonalizerTests.cs
    └── InpFileCanonalizerTests.cs
```

## Key Test Scenarios

### Security Testing
- ✅ Valid signatures verify successfully
- ✅ Modified licenses fail verification
- ✅ Invalid keys produce appropriate errors
- ✅ Different padding schemes work correctly
- ✅ Tamper detection catches any license modification

### Validation Testing
- ✅ Valid licenses pass all checks
- ✅ Missing required fields trigger errors
- ✅ Invalid field types are detected
- ✅ Expired licenses are rejected
- ✅ Default values are applied correctly

### Serialization Testing
- ✅ Round-trip JSON preserves all data
- ✅ Round-trip YAML preserves all data
- ✅ Canonical serialization is deterministic
- ✅ Complex types (lists, datetime) serialize correctly
- ✅ Schema information is preserved

### Processing Testing
- ✅ GUIDs are generated uniquely
- ✅ Timestamps reflect current time
- ✅ File hashes are computed correctly
- ✅ Processors execute in correct order
- ✅ Processor errors are handled gracefully

### Canonicalization Testing
- ✅ Comments are removed consistently
- ✅ Whitespace is normalized
- ✅ File semantics are preserved
- ✅ Hashes are deterministic after canonicalization
- ✅ Different file formats are handled correctly

## Adding New Tests

### Template for New Test Class

```csharp
using Xunit;
using Shouldly;
using SimpleLicense.Core;

namespace SimpleLicense.Tests
{
    /// <summary>
    /// Tests for [FeatureName] - [Brief description]
    /// </summary>
    public class NewFeatureTests
    {
        private readonly TestFixture _fixture;
        
        public NewFeatureTests()
        {
            // Set up common test data
            _fixture = new TestFixture();
        }
        
        [Fact]
        public void Method_Scenario_ExpectedBehavior()
        {
            // Arrange
            var input = CreateTestInput();
            
            // Act
            var result = MethodUnderTest(input);
            
            // Assert
            result.ShouldNotBeNull();
            result.Property.ShouldBe(expectedValue);
        }
        
        [Theory]
        [InlineData("input1", "expected1")]
        [InlineData("input2", "expected2")]
        public void Method_WithVariousInputs_ProducesCorrectOutput(
            string input, string expected)
        {
            // Arrange
            var sut = CreateSystemUnderTest();
            
            // Act
            var result = sut.Process(input);
            
            // Assert
            result.ShouldBe(expected);
        }
    }
}
```

## Continuous Integration

Tests run automatically on:
- Every commit to main branch
- All pull requests
- Scheduled nightly builds

See [Build Status Badge](https://github.com/tomjanus/SimpleLicense/actions/workflows/dotnet.yml) for current test status.

## Contributing

When adding new features:
1. Write tests first (TDD approach recommended)
2. Ensure all existing tests still pass
3. Aim for >80% code coverage
4. Follow existing naming conventions
5. Add summary comments to test classes
6. Group related tests together

## Troubleshooting

### Tests Fail Locally But Pass in CI
- Check .NET SDK version: `dotnet --version` (should be 9.0+)
- Clean and rebuild: `dotnet clean && dotnet build`
- Delete bin/obj folders and rebuild

### Flaky Tests
- Ensure tests don't depend on system time (use fixed dates)
- Avoid shared state between tests
- Check for file system dependencies

### Slow Tests
- Review file I/O operations
- Consider using in-memory test data
- Profile with `dotnet test --logger trx --verbosity detailed`

## Related Documentation

- [Main Project README](../../README.md)
- [Core Library Documentation](../../src/SimpleLicense/Core/README.md)
- [Examples](../../examples/README.md)
- [CLI Documentation](../../src/SimpleLicense/CLI/README.md)

---

**Test Coverage Target**: >80% line coverage across all core functionality  
**Last Updated**: January 2026  
**Maintainer**: Tomasz Janus [@tomjanus]
