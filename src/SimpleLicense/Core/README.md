# SimpleLicense.Core

The core library for creating, managing, validating, signing, and verifying software licenses in .NET applications.

## Overview

**SimpleLicense.Core** provides a complete license management system with:

- 📝 **Flexible License Documents** - Dynamic field-based licenses with validation
- 🏗️ **Schema-Driven Creation** - Define license structure with YAML/JSON schemas
- ✍️ **Digital Signatures** - RSA-based cryptographic signing and verification
- 🔒 **File Protection** - Hash-based file integrity validation
- 🎨 **Canonical Serialization** - Consistent JSON output for signatures
- ⚡ **Field Processors** - Automated field generation and transformation
- ✅ **Two-Tier Validation** - Field-level and schema-level validation

## Architecture

```
SimpleLicense.Core/
├── License.cs                      # Core license document class
├── LicenseCreator.cs               # Schema-based license creation
├── LicenseSerializer.cs            # JSON serialization with profiles
├── CanonicalLicenseSerializer.cs   # Deterministic serialization for signatures
├── LicenseSigner.cs                # RSA license signing
├── LicenseVerifier.cs              # RSA signature verification
├── Interfaces.cs                   # Core interfaces (IFileCanonicalizer)
│
├── Canonicalizers/                 # File normalization utilities
│   └── README.md                   # See Canonicalizers documentation
│
├── Cryptography/                   # RSA key generation
│   └── README.md                   # See Cryptography documentation
│
├── LicenseValidation/              # Validation system
│   ├── Schema.cs                   # License schema definitions
│   ├── LicenseValidator.cs         # Schema-level validation
│   ├── FieldValidators.cs          # Field-level validation
│   ├── FieldProcessors.cs          # Field transformation
│   ├── FieldSerializers.cs         # Custom type serialization
│   └── README.md                   # See LicenseValidation documentation
│
└── Utils/                          # Utility classes
    ├── Hashing.cs                  # SHA-256 hashing
    ├── FileIO.cs                   # File I/O abstraction
    ├── Encodings.cs                # Encoding resolution
    ├── Types.cs                    # Type checking utilities
    └── README.md                   # See Utils documentation
```

## Core Components

### License

The fundamental license document class representing a license as a collection of named fields with values.

**Location:** [License.cs](License.cs)

**Key Features:**
- Dynamic field-based structure (any field can be added)
- Case-insensitive field names
- Field-level validation using `FieldValidators` registry
- Mandatory fields: `LicenseId`, `ExpiryUtc`, `Signature`
- JSON serialization/deserialization
- Field serializers for custom types

**Basic Usage:**

```csharp
using SimpleLicense.Core;

// Create license
var license = new License();
license["LicenseId"] = Guid.NewGuid().ToString();
license["CustomerName"] = "Acme Corporation";
license["MaxUsers"] = 100;
license["ExpiryUtc"] = DateTime.UtcNow.AddYears(1);

// Access fields
string customer = (string)license["CustomerName"];
int maxUsers = (int)license["MaxUsers"];

// Serialize to JSON
string json = license.ToJson();

// Deserialize from JSON
var loaded = License.FromJson(json);

// Validate mandatory fields
license.EnsureMandatoryPresent();
```

**Field Validation:**

```csharp
// Validation happens automatically on field assignment
try
{
    license["MaxUsers"] = "not a number"; // Throws LicenseValidationException
}
catch (LicenseValidationException ex)
{
    Console.WriteLine(ex.Message);
}

// Or use SetField for explicit error handling
if (!license.SetField("MaxUsers", value, out string? error))
{
    Console.WriteLine($"Validation failed: {error}");
}
```

### LicenseCreator

Creates licenses based on schemas with support for field processors and default values.

**Location:** [LicenseCreator.cs](LicenseCreator.cs)

**Key Features:**
- Schema-driven license generation
- Automatic field processor execution
- Default value application
- Working directory support for file operations
- Optional schema validation
- Diagnostic event logging

**Basic Usage:**

```csharp
using SimpleLicense.Core;
using SimpleLicense.Core.LicenseValidation;

// Load schema
var schema = LicenseSchema.FromYaml(File.ReadAllText("schema.yaml"));

// Create license creator
var creator = new LicenseCreator();
creator.OnInfo += msg => Console.WriteLine($"[INFO] {msg}");

// Create license with field values
var license = creator.CreateLicense(
    schema: schema,
    fieldValues: new Dictionary<string, object?>
    {
        ["CustomerName"] = "Acme Corp",
        ["MaxUsers"] = 100,
        ["ConfigFile"] = "config.yaml" // Will be processed by HashFile processor
    },
    workingDirectory: "./release",
    validateSchema: true
);
```

**With Field Processors:**

```csharp
// Schema with processors
var schema = LicenseSchema.FromYaml(@"
name: FileProtectionLicense
fields:
  - name: LicenseId
    type: string
    required: true
    signed: true
    processor: GenerateGuid  # Auto-generates GUID
    
  - name: CreatedUtc
    type: datetime
    required: true
    signed: true
    processor: CurrentTimestamp  # Auto-generates timestamp
    
  - name: AppHash
    type: string
    required: true
    signed: true
    processor: HashFile  # Computes file hash
");

// Create license - processors run automatically
var license = creator.CreateLicense(
    schema,
    new Dictionary<string, object?>
    {
        ["AppHash"] = "app.exe"  // Input is filename, output is hash
    },
    workingDirectory: "./release"
);

// LicenseId and CreatedUtc are generated automatically
// AppHash contains SHA-256 hash of app.exe
```

### LicenseSerializer

JSON serialization and deserialization for licenses with configurable options.

**Location:** [LicenseSerializer.cs](LicenseSerializer.cs)

**Key Features:**
- Configurable JSON serialization profiles
- `JsonProfiles.Canonical` - For signatures (camelCase, no indentation)
- `JsonProfiles.Pretty` - For humans (camelCase, indented)
- Instance-level configuration
- UTF-8 byte array serialization

**Basic Usage:**

```csharp
using SimpleLicense.Core;

// Pretty-printed JSON (human-readable)
var prettySerializer = new LicenseSerializer { Options = JsonProfiles.Pretty };
string prettyJson = prettySerializer.SerializeLicenseDocument(license);
Console.WriteLine(prettyJson);

// Canonical JSON (for signatures)
var canonicalSerializer = new LicenseSerializer { Options = JsonProfiles.Canonical };
string canonicalJson = canonicalSerializer.SerializeLicenseDocument(license);

// Byte array serialization
byte[] bytes = prettySerializer.SerializeLicenseDocumentUtf8(license);

// Deserialization
var loadedLicense = prettySerializer.DeserializeLicenseDocument(prettyJson);
```

**Profiles Comparison:**

```csharp
// Pretty profile
{
  "licenseId": "ABC-123",
  "customerName": "Acme Corp",
  "maxUsers": 100
}

// Canonical profile
{"customerName":"Acme Corp","licenseId":"ABC-123","maxUsers":100}
```

### CanonicalLicenseSerializer

Deterministic JSON serialization for cryptographic signing and verification.

**Location:** [CanonicalLicenseSerializer.cs](CanonicalLicenseSerializer.cs)

**Key Features:**
- Deterministic output (same input always produces identical output)
- Alphabetically sorted keys
- No indentation or extra whitespace
- Automatic exclusion of `Signature` field
- Configurable field exclusion list
- UTF-8 byte array output

**Basic Usage:**

```csharp
using SimpleLicense.Core;

// Create canonical serializer
var serializer = new CanonicalLicenseSerializer();

// Serialize to canonical bytes (used for signing)
byte[] canonicalBytes = serializer.SerializeLicenseDocument(license);

// Deserialize
var loadedLicense = serializer.DeserializeLicenseDocument(canonicalBytes);
```

**Excluding Fields:**

```csharp
// Exclude additional fields from signature
var serializer = new CanonicalLicenseSerializer
{
    UnSerializedFields = new List<string> 
    { 
        "Metadata",      // Don't sign metadata
        "LastChecked"    // Don't sign client-side timestamps
    }
};

// Signature and UnSerializedFields are automatically excluded
byte[] canonicalBytes = serializer.SerializeLicenseDocument(license);
```

**Why Canonical Serialization?**

Digital signatures require **deterministic serialization** - the same license must always produce the same byte sequence. Canonical serialization ensures:

1. **Sorted keys** - Consistent field order
2. **No whitespace** - Removes formatting variations
3. **Consistent encoding** - Always UTF-8
4. **Excluded fields** - Signature field not included in signature computation

### LicenseSigner

Signs licenses using RSA private keys to ensure integrity and authenticity.

**Location:** [LicenseSigner.cs](LicenseSigner.cs)

**Key Features:**
- RSA digital signatures (2048/3072/4096-bit keys)
- SHA-256 hashing
- PSS and PKCS1 padding support
- Base64-encoded signatures
- Canonical serialization for consistency

**Basic Usage:**

```csharp
using SimpleLicense.Core;

// Load private key
string privateKey = File.ReadAllText("private_key.pem");

// Create signer (PSS padding by default)
var signer = new LicenseSigner(privateKey);

// Sign license (adds Signature field)
var signedLicense = signer.SignLicenseDocument(license);

// Signature is now in the license
string signature = (string)signedLicense["Signature"];
Console.WriteLine($"Signature: {signature}");
```

**Padding Options:**

```csharp
// PSS padding (recommended, default)
var signerPss = new LicenseSigner(
    privateKey, 
    padding: PaddingChoice.Pss
);

// PKCS1 padding (legacy compatibility)
var signerPkcs1 = new LicenseSigner(
    privateKey, 
    padding: PaddingChoice.Pkcs1
);
```

**Custom Serializer:**

```csharp
// Exclude additional fields from signature
var serializer = new CanonicalLicenseSerializer
{
    UnSerializedFields = new List<string> { "ClientMetadata" }
};

var signer = new LicenseSigner(
    privateKey,
    serializer: serializer
);
```

### LicenseVerifier

Verifies license signatures using RSA public keys to detect tampering.

**Location:** [LicenseVerifier.cs](LicenseVerifier.cs)

**Key Features:**
- RSA signature verification
- SHA-256 hash verification
- Tamper detection
- Detailed failure reasons
- JSON and object verification

**Basic Usage:**

```csharp
using SimpleLicense.Core;

// Load public key
string publicKey = File.ReadAllText("public_key.pem");

// Create verifier (PSS padding by default)
var verifier = new LicenseVerifier(publicKey);

// Verify JSON license
string licenseJson = File.ReadAllText("license.json");
bool isValid = verifier.VerifyLicenseDocumentJson(
    licenseJson, 
    out string? failureReason
);

if (isValid)
{
    Console.WriteLine("✓ License signature is valid");
}
else
{
    Console.WriteLine($"✗ Verification failed: {failureReason}");
}
```

**Verify License Object:**

```csharp
// If you already have a License object
var license = License.FromJson(licenseJson);

bool isValid = verifier.VerifyLicenseDocument(
    license, 
    out string? failureReason
);
```

**Failure Reasons:**

```csharp
if (!verifier.VerifyLicenseDocumentJson(licenseJson, out var reason))
{
    // Possible reasons:
    // - "Signature missing or empty"
    // - "Signature is not valid Base64"
    // - "Signature verification failed" (license was tampered)
    // - "Invalid JSON: ..."
    // - "RSA verification error: ..."
    Console.WriteLine($"Failed: {reason}");
}
```

### IFileCanonicalizer

Interface for file canonicalization - normalizing file content before hashing.

**Location:** [Interfaces.cs](Interfaces.cs)

**Purpose:** Defines a contract for normalizing file content by removing comments, standardizing whitespace, and applying domain-specific transformations.

**Interface Definition:**

```csharp
public interface IFileCanonicalizer
{
    /// <summary>
    /// Canonicalizes the raw content of a file.
    /// </summary>
    string Canonicalize(string input);

    /// <summary>
    /// File extensions this canonicalizer supports (e.g. ".inp", ".yaml")
    /// </summary>
    IEnumerable<string> SupportedExtensions { get; }
}
```

**Built-in Implementations:**

- `GenericTextFileCanonicalizer` - For general text files (YAML, JSON, CSV)
- `InpFileCanonicalizer` - For EPANET .inp network files

See [Canonicalizers README](Canonicalizers/README.md) for details.

**Usage Example:**

```csharp
using SimpleLicense.Core;
using SimpleLicense.Core.Canonicalizers;
using SimpleLicense.Core.Utils;

// Create canonicalizer
IFileCanonicalizer canonicalizer = new GenericTextFileCanonicalizer();

// Read and canonicalize file
string rawContent = File.ReadAllText("config.yaml");
string canonical = canonicalizer.Canonicalize(rawContent);

// Hash canonicalized content
string hash = TextFileHasher.Sha256Hex(canonical);

// Or use directly with HashFile
string fileHash = TextFileHasher.HashFile(
    "config.yaml",
    canonicalizer: canonicalizer
);
```

## Common Workflows

### Workflow 1: Simple License Creation and Signing

```csharp
using SimpleLicense.Core;
using SimpleLicense.Core.LicenseValidation;
using SimpleLicense.Core.Cryptography;

// 1. Generate RSA keys (one-time setup)
var keyGen = new KeyGenerator(keySize: 2048);
keyGen.KeyDir = new DirectoryInfo("./keys");
var result = keyGen.GenerateKeys();

// 2. Create license
var license = new License();
license["LicenseId"] = Guid.NewGuid().ToString();
license["CustomerName"] = "Acme Corporation";
license["MaxUsers"] = 100;
license["ExpiryUtc"] = DateTime.UtcNow.AddYears(1);

// 3. Sign license
string privateKey = File.ReadAllText(result.PrivateKeyPath);
var signer = new LicenseSigner(privateKey);
var signedLicense = signer.SignLicenseDocument(license);

// 4. Save license
var serializer = new LicenseSerializer { Options = JsonProfiles.Pretty };
string json = serializer.SerializeLicenseDocument(signedLicense);
File.WriteAllText("customer_license.json", json);

// 5. Customer verifies license
string publicKey = File.ReadAllText(result.PublicKeyPath);
var verifier = new LicenseVerifier(publicKey);
if (verifier.VerifyLicenseDocumentJson(json, out var reason))
{
    Console.WriteLine("✓ License is valid");
}
```

### Workflow 2: Schema-Based License with Processors

```csharp
using SimpleLicense.Core;
using SimpleLicense.Core.LicenseValidation;

// 1. Define schema
var schema = LicenseSchema.FromYaml(@"
name: FileProtectionLicense
fields:
  - name: LicenseId
    type: string
    required: true
    signed: true
    processor: GenerateGuid
    
  - name: CustomerName
    type: string
    required: true
    signed: true
    
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
    
  - name: ExpiryUtc
    type: datetime
    required: true
    signed: true
    
  - name: Signature
    type: string
    required: false
    signed: false
");

// 2. Create license with processors
var creator = new LicenseCreator();
var license = creator.CreateLicense(
    schema,
    fieldValues: new Dictionary<string, object?>
    {
        ["CustomerName"] = "Acme Corp",
        ["AppHash"] = "app.exe",      // Processor will hash this file
        ["ConfigHash"] = "config.xml", // Processor will hash this file
        ["ExpiryUtc"] = DateTime.UtcNow.AddYears(1)
    },
    workingDirectory: "./release",
    validateSchema: true
);

// LicenseId is auto-generated
// AppHash and ConfigHash contain file hashes

// 3. Sign and save
string privateKey = File.ReadAllText("private_key.pem");
var signer = new LicenseSigner(privateKey);
var signedLicense = signer.SignLicenseDocument(license);

var serializer = new LicenseSerializer { Options = JsonProfiles.Pretty };
File.WriteAllText("license.json", serializer.SerializeLicenseDocument(signedLicense));
```

### Workflow 3: License Validation at Application Startup

```csharp
using SimpleLicense.Core;
using SimpleLicense.Core.LicenseValidation;

public class LicenseManager
{
    private readonly string _publicKey;
    
    public LicenseManager(string publicKeyPem)
    {
        _publicKey = publicKeyPem;
    }
    
    public void ValidateApplicationLicense(string licensePath)
    {
        // 1. Load license
        if (!File.Exists(licensePath))
            throw new FileNotFoundException("License file not found");
        
        string licenseJson = File.ReadAllText(licensePath);
        
        // 2. Verify signature
        var verifier = new LicenseVerifier(_publicKey);
        if (!verifier.VerifyLicenseDocumentJson(licenseJson, out var reason))
            throw new UnauthorizedAccessException($"Invalid license: {reason}");
        
        // 3. Parse and validate
        var license = License.FromJson(licenseJson);
        
        // 4. Check expiration
        var expiry = (DateTime)license["ExpiryUtc"];
        if (DateTime.UtcNow > expiry)
            throw new UnauthorizedAccessException("License has expired");
        
        // 5. Check user limits
        int maxUsers = (int)license["MaxUsers"];
        int currentUsers = GetCurrentUserCount();
        if (currentUsers >= maxUsers)
            throw new UnauthorizedAccessException($"User limit reached ({maxUsers})");
        
        Console.WriteLine($"✓ License valid for: {license["CustomerName"]}");
    }
    
    private int GetCurrentUserCount() => 1; // Your implementation
}

// Usage
string publicKey = @"-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA...
-----END PUBLIC KEY-----";

var manager = new LicenseManager(publicKey);
try
{
    manager.ValidateApplicationLicense("license.json");
    // Application authorized - continue startup
}
catch (Exception ex)
{
    Console.WriteLine($"License validation failed: {ex.Message}");
    Environment.Exit(1);
}
```

## Sub-Modules

The Core module contains several specialized sub-modules, each with its own detailed documentation:

### 📁 [Canonicalizers](Canonicalizers/README.md)

File canonicalization for consistent hashing and validation.

**Components:**
- `GenericTextFileCanonicalizer` - For YAML, JSON, CSV, text files
- `InpFileCanonicalizer` - For EPANET .inp network files
- `CanonicalizerRegistry` - Dynamic canonicalizer registration

**Use Cases:**
- File protection licenses
- Configuration file validation
- Domain-specific file processing

### 🔐 [Cryptography](Cryptography/README.md)

RSA key generation, signing, and verification.

**Components:**
- `KeyGenerator` - RSA key pair generation (2048/3072/4096-bit)
- `LicenseSigner` - Digital signature creation
- `LicenseVerifier` - Signature verification

**Use Cases:**
- Secure license distribution
- Tamper detection
- License authentication

### ✅ [LicenseValidation](LicenseValidation/README.md)

Two-tier validation system for licenses.

**Components:**
- `LicenseValidator` - Schema-level structural validation
- `FieldValidators` - Field-level value validation
- `FieldProcessors` - Automated field generation/transformation
- `FieldSerializers` - Custom type serialization
- `LicenseSchema` - Schema definition and loading

**Use Cases:**
- Schema-driven license creation
- Field validation and normalization
- Automated license generation
- Custom field types

### 🛠️ [Utils](Utils/README.md)

Utility classes for common operations.

**Components:**
- `TextFileHasher` - SHA-256 file and string hashing
- `FileIO` / `IFileIO` - File I/O abstraction with encoding support
- `EncodingMap` - Encoding name resolution
- `TypeChecking` - Runtime type checking
- `IFileSource` - File enumeration abstractions

**Use Cases:**
- File hashing for integrity checks
- Cross-platform file operations
- Type validation
- Batch file processing

## Key Concepts

### Canonical Serialization

**What:** Deterministic JSON serialization where the same input always produces identical output.

**Why:** Required for digital signatures - signature is computed on canonical bytes, so verification requires identical byte sequence.

**How:**
- Keys sorted alphabetically
- No whitespace or indentation
- Consistent UTF-8 encoding
- Signature field excluded

**Example:**

```csharp
// Different input orders
var license1 = new License();
license1["B"] = 2;
license1["A"] = 1;

var license2 = new License();
license2["A"] = 1;
license2["B"] = 2;

var serializer = new CanonicalLicenseSerializer();

// Both produce identical canonical bytes
byte[] bytes1 = serializer.SerializeLicenseDocument(license1);
byte[] bytes2 = serializer.SerializeLicenseDocument(license2);

// bytes1 equals bytes2: {"A":1,"B":2}
```

### Field Processors

**What:** Functions that automatically generate or transform field values during license creation.

**Why:** Automates repetitive tasks (GUID generation, timestamps, file hashing) and ensures consistency.

**Built-in Processors:**
- `GenerateGuid` - Creates new GUID/UUID
- `CurrentTimestamp` - Current UTC timestamp
- `HashFile` - SHA-256 hash of single file
- `HashFiles` - SHA-256 hash of multiple files
- `ToUpper` / `ToLower` - String transformation
- `PassThrough` - Identity function

**Example:**

```csharp
// Schema with processors
var schema = LicenseSchema.FromYaml(@"
fields:
  - name: LicenseId
    processor: GenerateGuid  # Auto-generated
  - name: CreatedUtc
    processor: CurrentTimestamp  # Auto-generated
  - name: FileHash
    processor: HashFile  # User provides filename
");

// Only provide filename - hash computed automatically
var license = creator.CreateLicense(schema, new Dictionary<string, object?>
{
    ["FileHash"] = "app.exe"  // Input: filename, Output: hash
});

// LicenseId and CreatedUtc generated automatically
```

### Two-Tier Validation

**Tier 1: Field-Level** (FieldValidators)
- Validates individual field VALUES
- Applied when setting fields
- Ensures data quality (format, range, type)
- Example: "MaxUsers must be positive integer"

**Tier 2: Schema-Level** (LicenseValidator)
- Validates license STRUCTURE
- Applied when validating complete license
- Ensures structural compliance (required fields, types)
- Example: "CustomerName field is required"

**Example:**

```csharp
// Tier 1: Field validation on assignment
var license = new License();
license["MaxUsers"] = 50;      // ✓ Valid (field-level)
license["MaxUsers"] = "text";  // ✗ Throws (field-level)

// Tier 2: Schema validation on complete license
var schema = LicenseSchema.FromYaml("...");
var validator = new LicenseValidator(schema);

if (validator.Validate(license, out var errors))
{
    Console.WriteLine("✓ License valid");  // Schema-level
}
```

## Best Practices

### Key Management

✅ **DO:**
- Generate keys on secure machine
- Keep private keys in encrypted storage
- Use 2048-bit minimum (3072+ recommended)
- Distribute only public keys
- Rotate keys periodically

❌ **DON'T:**
- Commit private keys to version control
- Distribute private keys to customers
- Embed private keys in applications

### License Security

✅ **DO:**
- Always verify signatures before trusting license data
- Check expiration dates at application startup
- Validate file hashes for file protection licenses
- Use canonical serialization for signatures
- Store licenses outside application directory

❌ **DON'T:**
- Trust unsigned licenses
- Allow users to modify license files
- Skip signature verification
- Use non-canonical serialization for signing

### Schema Design

✅ **DO:**
- Include `LicenseId` with `GenerateGuid` processor
- Include `ExpiryUtc` for time-limited licenses
- Mark security-critical fields as `signed: true`
- Use processors for automated generation
- Provide clear field names and types

❌ **DON'T:**
- Sign non-critical fields unnecessarily
- Use complex nested structures
- Omit required field markers
- Forget to exclude `Signature` field from signing

### Error Handling

```csharp
public void SafeLicenseValidation(string licensePath)
{
    try
    {
        // Load and verify
        var json = File.ReadAllText(licensePath);
        var verifier = new LicenseVerifier(publicKey);
        
        if (!verifier.VerifyLicenseDocumentJson(json, out var reason))
        {
            // Specific failure reason
            LogError($"Signature verification failed: {reason}");
            throw new UnauthorizedAccessException("Invalid license");
        }
        
        // Parse and validate
        var license = License.FromJson(json);
        license.EnsureMandatoryPresent();
        
        // Check expiration
        var expiry = (DateTime)license["ExpiryUtc"];
        if (DateTime.UtcNow > expiry)
        {
            throw new UnauthorizedAccessException("License expired");
        }
    }
    catch (FileNotFoundException)
    {
        LogError("License file not found");
        throw;
    }
    catch (JsonException ex)
    {
        LogError($"Invalid license format: {ex.Message}");
        throw new UnauthorizedAccessException("Corrupted license");
    }
    catch (LicenseValidationException ex)
    {
        LogError($"License validation failed: {ex.Message}");
        throw;
    }
}
```

## Dependencies

- **.NET 9.0** - Core framework
- **System.Text.Json** - JSON serialization
- **System.Security.Cryptography** - RSA cryptography
- **YamlDotNet 16.3.0** - YAML schema support
- **NodaTime 3.2.2** - DateTime handling

## Examples

Complete working examples are available in the [examples/](../../../examples/) directory:

- **Example 1:** License Creation and Validation
- **Example 2:** File Canonicalization
- **Example 3:** Flexible License Documents
- **Example 4:** License Serialization
- **Example 5:** Field Serializers
- **Example 6:** Field Processors
- **Example 7:** Signing and Verification

See [Examples README](../../../examples/README.md) for details.

## Testing

Comprehensive test suite with 307+ tests:

```bash
cd tests/SimpleLicense.Tests
dotnet test
```

See [Tests README](../../../tests/SimpleLicense.Tests/README.md) for details.

## License

See [LICENSE](../../../LICENSE) file in the project root.
