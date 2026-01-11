# SimpleLicense

A flexible, schema-driven .NET library and CLI tool for creating, signing, validating, and managing software licenses with cryptographic signatures and file protection.

[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-12.0-239120)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## Overview

**SimpleLicense** provides a complete license management solution for .NET applications, combining:

- 📝 **Flexible License Documents** - Dynamic field-based licenses that can store any data
- 🏗️ **Schema-Driven Creation** - Define license structure once, generate many
- ✍️ **Digital Signatures** - RSA-based cryptographic signing for tamper detection
- 🔒 **File Protection** - Hash validation for configuration files and executables
- ⚡ **Field Processors** - Automated GUID generation, timestamps, file hashing
- 🎯 **Two-Tier Validation** - Field-level and schema-level validation
- 🖥️ **CLI Tool** - Command-line interface for license operations
- 🎨 **Canonical Serialization** - Consistent JSON output for reliable signatures

Perfect for commercial software, research tools, enterprise applications, or any scenario requiring license management.

## Key Features

### 🔐 Cryptographic Security
- **RSA Digital Signatures** - 2048/3072/4096-bit keys with PSS or PKCS1 padding
- **SHA-256 Hashing** - Secure file and content hashing
- **Tamper Detection** - Automatic verification of license integrity
- **Deterministic Serialization** - Canonical JSON for consistent signatures

### 📋 Schema-Driven Licenses
- **YAML/JSON Schemas** - Define license structure declaratively
- **Type System** - string, int, double, bool, datetime, list support
- **Required Fields** - Enforce mandatory license fields
- **Default Values** - Automatic field population
- **Field Processors** - Transform and generate field values

### ⚙️ Field Processors
- `GenerateGuid` - Auto-generate unique license IDs
- `CurrentTimestamp` - Add creation timestamps
- `HashFile` / `HashFiles` - Protect files with SHA-256 hashes
- `ToUpper` / `ToLower` - String transformations
- Custom processors - Extend with your own logic

### 🛡️ File Protection
- **Canonicalization** - Normalize files before hashing (remove comments, whitespace)
- **Generic Text Files** - YAML, JSON, CSV, configuration files
- **Domain-Specific** - EPANET .inp network files
- **Extensible** - Add custom canonicalizers for your file types

### 🖥️ Command Line Interface
- `info` - Display library version and information
- `generate-keys` - Create RSA key pairs
- `calculate-hash` - Compute file hashes
- `create-license` - Generate and sign licenses from schemas
- `validate-schema` - Verify schema structure
- `validate-license` - Verify license signatures and validity

## Quick Start

### Installation

```bash
# Clone repository
git clone https://github.com/yourusername/SimpleLicense.git
cd SimpleLicense

# Build solution
dotnet build

# Run tests
dotnet test

# Build CLI tool (optional)
cd src/SimpleLicense/CLI
dotnet build -c Release
```

### Basic Usage

#### 1. Generate RSA Keys

```bash
# Using CLI
simplelicense generate-keys --output ./keys --key-size 2048

# Or in C#
using SimpleLicense.Core.Cryptography;

var keyGen = new KeyGenerator(keySize: 2048);
keyGen.KeyDir = new DirectoryInfo("./keys");
var result = keyGen.GenerateKeys();
```

#### 2. Create a License Schema

Create `schema.yaml`:

```yaml
name: BasicLicense
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
    
  - name: MaxUsers
    type: int
    required: true
    signed: true
    
  - name: ExpiryUtc
    type: datetime
    required: true
    signed: true
    
  - name: Signature
    type: string
    required: false
    signed: false
```

#### 3. Create and Sign License

```bash
# Using CLI
simplelicense create-license \
  --schema schema.yaml \
  --private-key ./keys/private_key.pem \
  --output license.json \
  --field CustomerName="Acme Corporation" \
  --field MaxUsers=100 \
  --field ExpiryUtc=2027-12-31T23:59:59Z
```

Or in C#:

```csharp
using SimpleLicense.Core;
using SimpleLicense.Core.LicenseValidation;

// Load schema
var schema = LicenseSchema.FromYaml(File.ReadAllText("schema.yaml"));

// Create license
var creator = new LicenseCreator();
var license = creator.CreateLicense(
    schema,
    fieldValues: new Dictionary<string, object?>
    {
        ["CustomerName"] = "Acme Corporation",
        ["MaxUsers"] = 100,
        ["ExpiryUtc"] = new DateTime(2027, 12, 31, 23, 59, 59, DateTimeKind.Utc)
    }
);

// Sign license
string privateKey = File.ReadAllText("./keys/private_key.pem");
var signer = new LicenseSigner(privateKey);
var signedLicense = signer.SignLicenseDocument(license);

// Save
var serializer = new LicenseSerializer { Options = JsonProfiles.Pretty };
File.WriteAllText("license.json", serializer.SerializeLicenseDocument(signedLicense));
```

#### 4. Verify License

```bash
# Using CLI
simplelicense validate-license \
  --license license.json \
  --public-key ./keys/public_key.pem
```

Or in C#:

```csharp
using SimpleLicense.Core;

// Load and verify
string publicKey = File.ReadAllText("./keys/public_key.pem");
string licenseJson = File.ReadAllText("license.json");

var verifier = new LicenseVerifier(publicKey);
if (verifier.VerifyLicenseDocumentJson(licenseJson, out var reason))
{
    Console.WriteLine("✓ License is valid");
    var license = License.FromJson(licenseJson);
    Console.WriteLine($"Customer: {license["CustomerName"]}");
    Console.WriteLine($"Max Users: {license["MaxUsers"]}");
    Console.WriteLine($"Expires: {license["ExpiryUtc"]}");
}
else
{
    Console.WriteLine($"✗ License verification failed: {reason}");
}
```

## Project Structure

```
SimpleLicense/
├── src/
│   └── SimpleLicense/
│       ├── CLI/                    # Command-line interface
│       │   ├── CLI.cs             # CLI commands implementation
│       │   └── README.md          # CLI documentation
│       │
│       └── Core/                   # Core library
│           ├── License.cs          # License document class
│           ├── LicenseCreator.cs   # Schema-based creation
│           ├── LicenseSerializer.cs # JSON serialization
│           ├── LicenseSigner.cs    # RSA signing
│           ├── LicenseVerifier.cs  # Signature verification
│           ├── README.md           # Core documentation
│           │
│           ├── Canonicalizers/     # File normalization
│           │   └── README.md
│           ├── Cryptography/       # RSA key generation
│           │   └── README.md
│           ├── LicenseValidation/  # Validation system
│           │   └── README.md
│           └── Utils/              # Utility classes
│               └── README.md
│
├── examples/                       # Usage examples
│   ├── LicenseCreationExample.cs
│   ├── SigningAndVerificationExample.cs
│   ├── FlexibleLicenseExample.cs
│   └── README.md                  # Examples documentation
│
├── tests/
│   └── SimpleLicense.Tests/       # Unit tests (307+ tests)
│       └── README.md
│
└── docs/                          # Additional documentation
    └── LicenseValidation_UML.puml
```

## Documentation

### 📚 Core Library
- **[Core Module README](src/SimpleLicense/Core/README.md)** - Main library documentation
  - Architecture overview
  - Core components (License, LicenseCreator, Serializers, Signer, Verifier)
  - Common workflows
  - Best practices

### 🔧 Sub-Modules
- **[Canonicalizers](src/SimpleLicense/Core/Canonicalizers/README.md)** - File canonicalization
- **[Cryptography](src/SimpleLicense/Core/Cryptography/README.md)** - RSA key generation and signing
- **[LicenseValidation](src/SimpleLicense/Core/LicenseValidation/README.md)** - Validation system
- **[Utils](src/SimpleLicense/Core/Utils/README.md)** - Utility classes

### 🖥️ Command Line Interface
- **[CLI README](src/SimpleLicense/CLI/README.md)** - Complete CLI documentation
  - All commands and options
  - Usage examples
  - Tips and tricks

### 📖 Examples
- **[Examples README](examples/README.md)** - 7 comprehensive examples
  - License creation and validation
  - File canonicalization
  - Flexible license documents
  - Serialization
  - Field processors and serializers
  - Digital signatures

## Use Cases

### Commercial Software Licensing
```csharp
// Create trial license (30 days)
var license = new License();
license["LicenseId"] = Guid.NewGuid().ToString();
license["CustomerName"] = "Trial User";
license["LicenseType"] = "Trial";
license["MaxUsers"] = 1;
license["ExpiryUtc"] = DateTime.UtcNow.AddDays(30);
license["Features"] = new[] { "BasicFeatures" };

// Sign and distribute
var signer = new LicenseSigner(privateKey);
var signedLicense = signer.SignLicenseDocument(license);
```

### File Protection Licenses
```csharp
// Protect application and config files
var schema = LicenseSchema.FromYaml(@"
fields:
  - name: AppHash
    processor: HashFile
  - name: ConfigHash
    processor: HashFile
");

var license = creator.CreateLicense(schema, new Dictionary<string, object?>
{
    ["AppHash"] = "MyApp.exe",
    ["ConfigHash"] = "config.xml"
}, workingDirectory: "./release");

// At runtime, verify files haven't been modified
string currentAppHash = TextFileHasher.HashFile("MyApp.exe");
if (currentAppHash != (string)license["AppHash"])
{
    throw new UnauthorizedAccessException("Application has been modified");
}
```

### Research Software Licensing
```csharp
// License with dataset and model file protection
var license = new License();
license["ResearcherName"] = "Dr. Jane Smith";
license["InstitutionId"] = "MIT-2026-001";
license["DatasetHash"] = TextFileHasher.HashFile("dataset.csv");
license["ModelHash"] = TextFileHasher.HashFile("model.h5");
license["ExpiryUtc"] = DateTime.UtcNow.AddYears(1);
license["AllowedPublications"] = 5;
```

### Enterprise Application Licensing
```csharp
// Multi-feature license with user limits
var license = new License();
license["CompanyId"] = "ACME-CORP-2026";
license["MaxUsers"] = 500;
license["MaxConcurrentSessions"] = 100;
license["EnabledModules"] = new[] { "Analytics", "Reporting", "API" };
license["SupportLevel"] = "Enterprise";
license["ExpiryUtc"] = DateTime.UtcNow.AddYears(1);
```

## Advanced Features

### Custom Field Processors

```csharp
using SimpleLicense.Core.LicenseValidation;

// Register custom processor
FieldProcessors.Registry["GenerateMachineId"] = (ctx, value) =>
{
    // Generate hardware-locked license
    return Environment.MachineName + "-" + GetMachineId();
};

// Use in schema
var schema = LicenseSchema.FromYaml(@"
fields:
  - name: MachineId
    processor: GenerateMachineId
");
```

### Custom Field Validators

```csharp
using SimpleLicense.Core.LicenseValidation;

// Register custom validator
FieldValidators.Registry["MaxUsers"] = (value, out string? error) =>
{
    if (value is int count && count >= 1 && count <= 10000)
    {
        error = null;
        return true;
    }
    error = "MaxUsers must be between 1 and 10000";
    return false;
};
```

### Custom File Canonicalizers

```csharp
using SimpleLicense.Core;

public class XmlFileCanonicalizer : IFileCanonicalizer
{
    public string Canonicalize(string input)
    {
        // Remove XML comments and normalize whitespace
        var doc = XDocument.Parse(input);
        doc.DescendantNodes()
           .OfType<XComment>()
           .Remove();
        return doc.ToString(SaveOptions.DisableFormatting);
    }
    
    public IEnumerable<string> SupportedExtensions => 
        new[] { ".xml", ".config" };
}

// Register and use
CanonicalizerRegistry.Register(new XmlFileCanonicalizer());
```

## Building and Testing

### Build

```bash
# Build entire solution
dotnet build

# Build in Release mode
dotnet build -c Release

# Build specific project
dotnet build src/SimpleLicense/Core/SimpleLicense.Core.csproj
```

### Test

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test file
dotnet test --filter "FullyQualifiedName~LicenseSigningAndVerificationTests"

# Verbose output
dotnet test --logger "console;verbosity=detailed"
```

### CLI Tool

```bash
# Build CLI tool
cd src/SimpleLicense/CLI
dotnet build -c Release

# Run directly
dotnet run -- info

# Or publish as standalone executable
dotnet publish -c Release -r linux-x64 --self-contained
```

## Dependencies

- **.NET 9.0** - Core framework
- **System.Text.Json** - JSON serialization
- **System.Security.Cryptography** - RSA cryptography
- **YamlDotNet 16.3.0** - YAML schema support
- **NodaTime 3.2.2** - DateTime handling
- **Spectre.Console 0.49.2** - CLI formatting

## Contributing

Contributions are welcome! Please feel free to:

1. **Report Issues** - Bug reports, feature requests
2. **Submit Pull Requests** - Code improvements, new features
3. **Improve Documentation** - Clarifications, examples
4. **Share Use Cases** - How you're using SimpleLicense

### Development Guidelines

- Follow existing code style and conventions
- Add unit tests for new features
- Update documentation as needed
- Ensure all tests pass before submitting PR

## License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

Built with:
- [.NET 9.0](https://dotnet.microsoft.com/)
- [YamlDotNet](https://github.com/aaubry/YamlDotNet)
- [NodaTime](https://nodatime.org/)
- [Spectre.Console](https://spectreconsole.net/)

## Support

- **Documentation**: See [src/SimpleLicense/Core/README.md](src/SimpleLicense/Core/README.md)
- **Examples**: See [examples/README.md](examples/README.md)
- **Issues**: Please report issues on the GitHub repository

---

**SimpleLicense** - Simple, secure, flexible license management for .NET applications.
