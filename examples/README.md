# SimpleLicense Examples

This directory contains a comprehensive suite of examples demonstrating all features of the SimpleLicense library. Each example is self-contained and shows best practices for using the library in real-world scenarios.

## Quick Start

### Run All Examples

```bash
cd examples
dotnet run
```

This will execute all 7 examples in sequence with colorized output.

### List Available Examples

```bash
dotnet run list
```

### Run Specific Example

To run a specific example, modify [Examples.cs](Examples.cs) and comment out the examples you don't want to run.

## Examples Overview

### Example 1: License Creation and Validation

**File:** [LicenseCreationExample.cs](LicenseCreationExample.cs)

**Demonstrates:**
- Creating licenses from JSON
- Field-level validation (required fields, data types)
- Schema-level validation (JSON and YAML formats)
- Working with license schemas programmatically

**Key Concepts:**
```csharp
// Create license from JSON
var license = License.FromJson(jsonString);

// Define schema
var schema = new LicenseSchema
{
    Name = "MySchema",
    Fields = new List<FieldDescriptor> { /* ... */ }
};

// Validate against schema
var validator = new LicenseValidator(schema);
if (validator.Validate(license, out var errors))
{
    // License is valid
}
```

**Outputs:**
- `outputs/user_license_schema.json` - Example schema in JSON format
- `outputs/user_license_schema.yaml` - Example schema in YAML format

### Example 2: File Canonicalization

**File:** [CanonicalizerExample.cs](CanonicalizerExample.cs)

**Demonstrates:**
- Canonicalizing YAML config files (removes comments, normalizes whitespace)
- Canonicalizing EPANET .inp network files (domain-specific rules)
- CSV file canonicalization
- Before/after comparison of canonicalization

**Key Concepts:**
```csharp
// Generic text file canonicalization
var canonicalizer = new GenericTextFileCanonicalizer();
var canonicalConfig = canonicalizer.Canonicalize(yamlContent);

// Domain-specific canonicalization (EPANET)
var inpCanonicalizer = new InpFileCanonicalizer();
var canonicalInp = inpCanonicalizer.Canonicalize(inpContent);
```

**Outputs:**
- `outputs/canonical_config.yaml` - Canonicalized YAML
- `outputs/canonical_network.inp` - Canonicalized EPANET file
- `outputs/canonical_data.csv` - Canonicalized CSV

**Use Case:** File protection licenses - ensure files haven't been modified by comparing hashes of canonical forms.

### Example 3: Flexible License Document

**File:** [FlexibleLicenseExample.cs](FlexibleLicenseExample.cs)

**Demonstrates:**
- Creating licenses with arbitrary fields
- Adding, updating, and removing fields dynamically
- Type conversions (string, int, double, bool)
- Null value handling
- Accessing fields with different data types

**Key Concepts:**
```csharp
// Create flexible license
var license = new License();

// Add various field types
license["CompanyName"] = "Acme Corp";
license["MaxUsers"] = 100;
license["Price"] = 299.99;
license["IsActive"] = true;

// Access with type safety
var maxUsers = (int)license["MaxUsers"];
var price = (double)license["Price"];
```

**Use Case:** Dynamic license systems where fields are determined at runtime.

### Example 4: License Serialization

**File:** [SerializationExample.cs](SerializationExample.cs)

**Demonstrates:**
- JSON serialization (compact and pretty formats)
- Canonical serialization (for signatures)
- Loading licenses from JSON
- Roundtrip serialization (serialize → deserialize → serialize)
- Comparing different serialization formats

**Key Concepts:**
```csharp
// Pretty JSON (human-readable)
var serializer = new LicenseSerializer 
{ 
    Options = JsonProfiles.Pretty 
};
var json = serializer.SerializeLicenseDocument(license);

// Canonical JSON (for signatures)
var canonicalSerializer = new CanonicalLicenseSerializer();
var canonical = canonicalSerializer.SerializeLicenseDocument(license);
```

**Outputs:**
- `outputs/signed_license.json` - Pretty-printed license
- Console output showing different serialization formats

**Use Case:** Understanding how licenses are stored and transmitted.

### Example 5: Field Serializers

**File:** [FieldSerializerExample.cs](FieldSerializerExample.cs)

**Demonstrates:**
- Registering custom field serializers
- Converting field values to strings
- Date/time serialization (NodaTime Instant)
- Type-specific serialization logic
- Fallback to default ToString()

**Key Concepts:**
```csharp
// Register custom serializer
FieldSerializers.Register<Instant>(instant => 
    InstantPattern.ExtendedIso.Format(instant)
);

// Serialize field with custom logic
var serialized = FieldSerializers.Serialize(instantValue);
```

**Use Case:** Custom data types in licenses (dates, enums, complex objects).

### Example 6: Field Processors

**File:** [FieldProcessorExample.cs](FieldProcessorExample.cs)

**Demonstrates:**
- Using built-in field processors (GenerateGuid, CurrentTimestamp, HashFile, HashFiles)
- Registering custom processors
- Schema-driven license creation with processors
- Automatic field value generation
- File hash calculation for license protection

**Key Concepts:**
```csharp
// Register custom processor
FieldProcessors.Register("MyProcessor", (context) => 
{
    var input = context.InputValue?.ToString() ?? "";
    return input.ToUpper();
});

// Use in schema
var schema = new LicenseSchema
{
    Fields = new List<FieldDescriptor>
    {
        new() { Name = "LicenseId", Processor = "GenerateGuid" },
        new() { Name = "CreatedUtc", Processor = "CurrentTimestamp" },
        new() { Name = "ConfigHash", Processor = "HashFile" }
    }
};

// Create license with automatic processing
var creator = new LicenseCreator();
var license = creator.CreateLicense(
    schema, 
    fieldValues: new() { ["ConfigHash"] = "config.txt" },
    workingDirectory: "./data"
);
```

**Outputs:**
- `outputs/processed_license.json` - License with processed fields
- `outputs/file_protection_schema.yaml` - Schema with processors

**Built-in Processors:**
- `GenerateGuid` - Creates new GUID/UUID
- `CurrentTimestamp` - Current UTC timestamp (ISO 8601)
- `HashFile` - SHA256 hash of single file
- `HashFiles` - SHA256 hash of multiple files (comma-separated)
- `ToUpper` - Convert to uppercase
- `ToLower` - Convert to lowercase
- `PassThrough` - Return value unchanged

**Use Case:** Automated license generation, file protection, audit trails.

### Example 7: Signing and Verification

**File:** [SigningAndVerificationExample.cs](SigningAndVerificationExample.cs)

**Demonstrates:**
- Generating RSA key pairs (2048-bit)
- Signing licenses with private keys
- Verifying licenses with public keys
- Detecting tampered licenses
- Canonical serialization for signature consistency
- JSON roundtrip with signature preservation
- Wrong key detection

**Key Concepts:**
```csharp
// Generate RSA keys
using var rsa = RSA.Create(2048);
var privateKey = rsa.ExportRSAPrivateKeyPem();
var publicKey = rsa.ExportRSAPublicKeyPem();

// Sign license
var signer = new LicenseSigner(privateKey);
var signedLicense = signer.SignLicenseDocument(license);

// Verify signature
var verifier = new LicenseVerifier(publicKey);
bool isValid = verifier.VerifyLicenseDocument(
    signedLicense, 
    out string? failureReason
);

// Detect tampering
signedLicense["MaxUsers"] = 999; // Tamper
isValid = verifier.VerifyLicenseDocument(signedLicense, out _);
// isValid will be false
```

**Key Features:**
- RSA-2048 encryption (supports 2048/3072/4096-bit)
- SHA256 hashing
- PSS and PKCS1 padding support
- Tamper-evident signatures
- PEM format keys

**Use Case:** Secure license distribution, preventing license modification.

## Common Patterns

### Pattern 1: Simple License Creation

```csharp
// 1. Define schema
var schema = LicenseSchema.FromYaml(@"
name: MyProductLicense
fields:
  - name: LicenseId
    type: string
    required: true
    signed: true
  - name: CustomerName
    type: string
    required: true
    signed: true
");

// 2. Create license
var creator = new LicenseCreator();
var license = creator.CreateLicense(
    schema,
    fieldValues: new() 
    { 
        ["LicenseId"] = "ABC-123",
        ["CustomerName"] = "Acme Corp"
    }
);

// 3. Validate
var validator = new LicenseValidator(schema);
validator.Validate(license, out var errors);
```

### Pattern 2: Signed License Workflow

```csharp
// 1. Generate keys (once)
var keyGen = new KeyGenerator(keySize: 2048);
var result = keyGen.GenerateKeys();
// Keys saved to keyGen.KeyDir

// 2. Create and sign license
var privateKey = File.ReadAllText("private_key.pem");
var signer = new LicenseSigner(privateKey);
var signedLicense = signer.SignLicenseDocument(license);

// 3. Distribute license + public key to customer

// 4. Customer verifies license
var publicKey = File.ReadAllText("public_key.pem");
var verifier = new LicenseVerifier(publicKey);
if (!verifier.VerifyLicenseDocumentJson(licenseJson, out var reason))
{
    throw new Exception($"Invalid license: {reason}");
}
```

### Pattern 3: File Protection License

```csharp
// 1. Create schema with file hash processor
var schema = LicenseSchema.FromYaml(@"
name: FileProtectionLicense
fields:
  - name: LicenseId
    type: string
    required: true
    signed: true
    processor: GenerateGuid
  - name: AppHash
    type: string
    required: true
    signed: true
    processor: HashFile
  - name: ConfigHash
    type: string
    required: true
    signed: true
    processor: HashFile
");

// 2. Create license (hashes calculated automatically)
var creator = new LicenseCreator();
var license = creator.CreateLicense(
    schema,
    fieldValues: new() 
    {
        ["AppHash"] = "app.exe",
        ["ConfigHash"] = "config.xml"
    },
    workingDirectory: "./release"
);

// 3. Sign license
var signer = new LicenseSigner(privateKey);
var signedLicense = signer.SignLicenseDocument(license);

// 4. On app startup, verify files haven't changed
// Recalculate hashes and compare with license values
```

## Output Files

All examples generate output files in the `outputs/` directory:

- **user_license_schema.json** - Example schema (JSON)
- **user_license_schema.yaml** - Example schema (YAML)
- **canonical_config.yaml** - Canonicalized YAML config
- **canonical_network.inp** - Canonicalized EPANET file
- **canonical_data.csv** - Canonicalized CSV
- **signed_license.json** - Signed license example
- **processed_license.json** - License with processed fields
- **file_protection_schema.yaml** - Schema for file protection
- **validated_user_license.json** - Validated license

## Data Files

The `data/` directory contains sample files used by examples:

- **file1.txt**, **file2.txt** - Sample text files for hashing
- **example_user_license.lic** - Sample license file
- **key_*_private.pem** - Private key (if generated)
- **key_*_public.pem** - Public key (if generated)

## Schema Files

- **cli_test_schema.yaml** - Test schema for CLI examples

## Building and Running

### Prerequisites

- .NET 9.0 SDK or later
- SimpleLicense.Core library (included as project reference)

### Build

```bash
cd examples
dotnet build
```

### Run

```bash
dotnet run
```

Or run the compiled executable:

```bash
./bin/Debug/net9.0/Examples
```

## Learning Path

**Recommended order for learning:**

1. **Start here:** Example 1 (License Creation and Validation)
   - Learn basic license structure and validation

2. **Then:** Example 4 (Serialization)
   - Understand how licenses are stored

3. **Next:** Example 7 (Signing and Verification)
   - Learn secure license distribution

4. **After that:** Example 6 (Field Processors)
   - Discover automated license generation

5. **Advanced:** Examples 2, 3, 5
   - Specialized topics (canonicalization, custom types)

## Integration with CLI

These examples show the library API. For command-line usage, see:

- **CLI Tool:** [src/SimpleLicense/CLI/](../src/SimpleLicense/CLI/)
- **CLI README:** [CLI Documentation](../src/SimpleLicense/CLI/README.md)

**Example: CLI equivalent of Example 7**

Library code:
```csharp
var creator = new LicenseCreator();
var license = creator.CreateLicense(schema, fieldValues);
var signer = new LicenseSigner(privateKey);
var signedLicense = signer.SignLicenseDocument(license);
```

CLI equivalent:
```bash
slic create-license \
    -s schema.yaml \
    --field CustomerName="Acme Corp" \
    --field MaxUsers=50 \
    -k private_key.pem \
    -o license.json
```

## Tips for Developers

### Debugging Examples

Enable verbose output by modifying examples:

```csharp
var creator = new LicenseCreator();
creator.OnInfo += msg => Console.WriteLine($"[INFO] {msg}");
```

### Creating Custom Examples

1. Create new file: `MyExample.cs`
2. Implement `public static void Run()` method
3. Add to [Examples.cs](Examples.cs):
   ```csharp
   RunExample("My Example", 8, MyExample.Run);
   ```

### Testing Examples

```bash
# Run with verbose output
dotnet run

# Check exit code
echo $?  # Should be 0 if all examples succeeded
```

## Troubleshooting

### "File not found" Errors

Make sure you're running from the examples directory:

```bash
cd examples
dotnet run
```

### Output Files Not Created

The `outputs/` directory is created automatically. If files aren't appearing:

```bash
# Check if directory exists
ls -la outputs/

# Check write permissions
touch outputs/test.txt
rm outputs/test.txt
```

### Key Generation Fails

RSA key generation requires sufficient entropy. On Linux/macOS:

```bash
# Check entropy
cat /proc/sys/kernel/random/entropy_avail  # Should be > 1000
```

### Examples Crash

Check if SimpleLicense.Core is built:

```bash
cd ../src/SimpleLicense/Core
dotnet build
cd ../../../examples
dotnet run
```

## More Information

- **Main Project:** [SimpleLicense README](../README.md)
- **Core Library:** [SimpleLicense.Core](../src/SimpleLicense/Core/README.md)
- **CLI Tool:** [SimpleLicense CLI](../src/SimpleLicense/CLI/README.md)
- **Tests:** [SimpleLicense.Tests](../tests/SimpleLicense.Tests/)

## Contributing

To add new examples:

1. Create example file following the pattern of existing examples
2. Add summary documentation at the top
3. Use `ConsoleHelper` for colored output
4. Update this README with example description
5. Add to `Examples.cs` main runner

## License

See [LICENSE](../LICENSE) file in the project root.
