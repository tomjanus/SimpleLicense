# SimpleLicense CLI (SLiC)

**Simple License Creator (SLiC)** is a command-line tool for generating, signing, and validating JSON-based software licenses for research-grade projects.

## Features

- 🔑 **RSA Key Generation** - Generate cryptographic key pairs for license signing
- 📝 **Schema-Driven License Creation** - Create licenses from flexible YAML/JSON schemas
- ✍️ **Digital Signatures** - Sign licenses with RSA-2048/3072/4096 encryption
- ✅ **License Validation** - Verify signatures and validate against schemas
- 🔒 **File Hashing** - Calculate SHA256 hashes for file protection
- 🎨 **Beautiful CLI** - Colorized output and progress indicators

## Installation

### Build from Source

```bash
cd src/SimpleLicense/CLI
dotnet build -c Release
```

The executable will be at: `bin/Release/net9.0/slic`

### Add to PATH (Optional)

```bash
# Linux/macOS
export PATH="$PATH:/path/to/SimpleLicense/src/SimpleLicense/CLI/bin/Release/net9.0"

# Or create a symlink
sudo ln -s /path/to/slic /usr/local/bin/slic
```

## Quick Start

### 1. Generate RSA Keys

```bash
slic generate-keys -k 2048 -d ./keys
```

This creates:
- `keys/key_2048bit_YYYYMMDD_private.pem` - Private key (keep secure!)
- `keys/key_2048bit_YYYYMMDD_public.pem` - Public key (distribute with app)

### 2. Create a License Schema

Create `schema.yaml`:

```yaml
name: MyProductLicense
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

### 3. Create a License

Using the **easy syntax** (recommended):

```bash
slic create-license \
    -s schema.yaml \
    --field CustomerName="Acme Corporation" \
    --field MaxUsers=100 \
    --field ExpiryUtc=2027-12-31T23:59:59Z \
    -k ./keys/key_2048bit_*_private.pem \
    -o license.json
```

### 4. Validate the License

```bash
slic validate-license \
    -l license.json \
    -k ./keys/key_2048bit_*_public.pem \
    -s schema.yaml
```

## Commands

### `info`

Display help information and command examples.

```bash
slic info
```

### `generate-keys`

Generate an RSA key pair for license signing.

**Options:**
- `-k, --keysize <BITS>` - Key size in bits (default: 2048, minimum: 2048)
- `-d, --keydir <PATH>` - Directory to save keys (default: current directory)
- `-v, --verbose` - Enable verbose output

**Examples:**

```bash
# Generate 2048-bit keys
slic generate-keys -d ./keys

# Generate 4096-bit keys for extra security
slic generate-keys -k 4096 -d ./keys
```

### `create-license`

Create and sign a license from a schema.

**Options:**
- `-s, --schema <PATH>` - Path to schema file (JSON or YAML) [required]
- `--field <KEY=VALUE>` - Individual field (can be repeated) [recommended]
- `--field-values <JSON>` - Field values as JSON string [legacy]
- `-f, --values-file <PATH>` - Path to JSON file with field values
- `-k, --keyfile <PATH>` - Path to private key file [required]
- `-o, --output <PATH>` - Output license file path (default: license.json)
- `-w, --workdir <PATH>` - Working directory for file processors (default: .)
- `--no-validate` - Skip schema validation
- `-v, --verbose` - Enable verbose output

**Examples:**

```bash
# Easy syntax with individual fields (RECOMMENDED)
slic create-license \
    -s schema.yaml \
    --field LicenseId=ABC-123 \
    --field CustomerName="Acme Corp" \
    --field MaxUsers=50 \
    --field ExpiryUtc=2027-12-31T23:59:59Z \
    -k private_key.pem \
    -o license.json

# From a JSON file
slic create-license \
    -s schema.yaml \
    -f field_values.json \
    -k private_key.pem \
    -o license.json

# With verbose output
slic create-license \
    -s schema.yaml \
    --field CustomerName="Test User" \
    --field MaxUsers=10 \
    -k private_key.pem \
    -o license.json \
    -v
```

**Field Value Types:**

The CLI automatically detects types:

```bash
--field MaxUsers=50              # Integer
--field Price=19.99              # Double
--field IsActive=true            # Boolean
--field IsActive=false           # Boolean
--field MiddleName=null          # Null
--field CompanyName="Acme Corp"  # String (quotes optional unless spaces)
```

### `validate-schema`

Validate a license schema file.

**Options:**
- `-s, --schema <PATH>` - Path to schema file (JSON or YAML) [required]
- `-v, --verbose` - Enable verbose output

**Examples:**

```bash
# Validate a schema
slic validate-schema -s schema.yaml

# Works with JSON too
slic validate-schema -s schema.json
```

### `validate-license`

Validate and verify a signed license file.

**Options:**
- `-l, --license <PATH>` - Path to license file [required]
- `-k, --public-key-file <PATH>` - Path to public key (for signature verification)
- `-s, --schema <PATH>` - Path to schema (for structure validation)
- `-v, --verbose` - Enable verbose output

**Examples:**

```bash
# Verify signature only
slic validate-license \
    -l license.json \
    -k public_key.pem

# Validate against schema only
slic validate-license \
    -l license.json \
    -s schema.yaml

# Full validation (signature + schema)
slic validate-license \
    -l license.json \
    -k public_key.pem \
    -s schema.yaml

# Check JSON syntax only
slic validate-license -l license.json
```

### `calculate-hash`

Calculate SHA256 hash of one or more files.

**Options:**
- `-i, --input <PATH>` - Input file(s) - repeat for multiple files [required]
- `-e, --encoding <NAME>` - Text encoding (default: utf-8)
- `-v, --verbose` - Enable verbose output

**Supported encodings:** utf-8, utf-16, utf-16be, ascii

**Examples:**

```bash
# Hash a single file
slic calculate-hash -i config.txt

# Hash multiple files
slic calculate-hash \
    -i file1.txt \
    -i file2.txt \
    -i file3.txt

# With specific encoding
slic calculate-hash \
    -i data.txt \
    -e utf-16
```

## Field Processors

Schemas can specify processors to automatically generate or transform field values:

- **GenerateGuid** - Generates a new GUID/UUID
- **CurrentTimestamp** - Generates current UTC timestamp
- **HashFile** - Calculates hash of a single file
- **HashFiles** - Calculates combined hash of multiple files
- **ToUpper** - Converts string to uppercase
- **ToLower** - Converts string to lowercase
- **PassThrough** - Returns value unchanged

**Example Schema with Processors:**

```yaml
name: FileProtectionLicense
fields:
  - name: LicenseId
    type: string
    required: true
    signed: true
    processor: GenerateGuid  # Auto-generated
  
  - name: CreatedUtc
    type: datetime
    required: true
    signed: true
    processor: CurrentTimestamp  # Auto-generated
  
  - name: ConfigFileHash
    type: string
    required: true
    signed: true
    processor: HashFile  # User provides filename
  
  - name: CustomerName
    type: string
    required: true
    signed: true  # User provides value
```

**Creating License with Processors:**

```bash
slic create-license \
    -s file_protection_schema.yaml \
    --field CustomerName="Acme Corp" \
    --field ConfigFileHash=config.txt \
    -k private_key.pem \
    -o license.json \
    -w /path/to/files
```

The processor will automatically:
- Generate a GUID for `LicenseId`
- Set current timestamp for `CreatedUtc`
- Calculate hash of `config.txt` for `ConfigFileHash`

## Common Workflows

### Workflow 1: Simple License

```bash
# 1. Generate keys
slic generate-keys -d ./keys

# 2. Create license
slic create-license \
    -s schema.yaml \
    --field CustomerName="Customer Inc" \
    --field MaxUsers=50 \
    -k ./keys/key_*_private.pem \
    -o customer_license.json

# 3. Distribute license and public key to customer
```

### Workflow 2: File-Protected License

```bash
# 1. Calculate hash of protected files
slic calculate-hash -i app.exe -i config.dll

# 2. Create license with file hashes
slic create-license \
    -s file_protection_schema.yaml \
    --field CustomerName="Customer Inc" \
    --field AppHash=app.exe \
    --field ConfigHash=config.dll \
    -k private_key.pem \
    -o license.json \
    -w ./release

# 3. Customer validates on startup
slic validate-license \
    -l license.json \
    -k public_key.pem \
    -s schema.yaml
```

### Workflow 3: License Renewal

```bash
# Original license expires, create new one
slic create-license \
    -s schema.yaml \
    --field LicenseId=EXISTING-ID-123 \
    --field CustomerName="Customer Inc" \
    --field ExpiryUtc=2028-12-31T23:59:59Z \
    -k private_key.pem \
    -o renewed_license.json
```

## Tips & Best Practices

### Security

- ✅ **Keep private keys secure** - Never distribute private keys
- ✅ **Use strong key sizes** - 2048-bit minimum, 4096-bit for high security
- ✅ **Validate licenses** - Always verify signatures before trusting license data
- ✅ **Protect license files** - Customers should not be able to modify licenses

### Field Naming

- Use PascalCase for field names: `CustomerName`, `MaxUsers`, `ExpiryUtc`
- Include `Utc` suffix for UTC timestamps: `CreatedUtc`, `ExpiryUtc`
- Use descriptive names: `MaxConcurrentUsers` vs `Max`

### Schema Design

```yaml
# Always include these standard fields
fields:
  - name: LicenseId
    type: string
    required: true
    signed: true
    processor: GenerateGuid  # Unique identifier
  
  - name: IssuedUtc
    type: datetime
    required: true
    signed: true
    processor: CurrentTimestamp  # When license was created
  
  - name: ExpiryUtc
    type: datetime
    required: true
    signed: true  # When license expires
  
  - name: Signature
    type: string
    required: false
    signed: false  # Digital signature field
```

### Testing

```bash
# Validate schema before creating licenses
slic validate-schema -s schema.yaml

# Create test license with verbose output
slic create-license \
    -s schema.yaml \
    --field CustomerName="TEST" \
    --field MaxUsers=1 \
    -k test_private.pem \
    -o test_license.json \
    -v

# Verify it works
slic validate-license \
    -l test_license.json \
    -k test_public.pem \
    -s schema.yaml \
    -v
```

## Troubleshooting

#### *Private key file not found*

Make sure the path to your key file is correct:

```bash
# Use full path
slic create-license -k /home/user/keys/private_key.pem ...

# Or relative path
slic create-license -k ./keys/key_2048bit_*_private.pem ...
```

#### *License validation failed: Required field 'X' is missing*

The schema requires this field. Add it:

```bash
slic create-license \
    --field X=value \
    ...
```

Or check if it should have a processor in the schema.

#### *Invalid field format: 'X'*

The `--field` option requires KEY=VALUE format:

```bash
# ❌ Wrong
--field "CustomerName Acme Corp"

# ✅ Correct
--field CustomerName="Acme Corp"
```

#### *JSON parsing error*

Check your JSON syntax if using `--field-values` or `-f`:

```bash
# Use --field instead (easier)
--field Key=Value

# Or validate JSON
echo '{"Key":"Value"}' | jq .
```

## Integration Examples

### C# Application

```csharp
using SimpleLicense.Core;
using SimpleLicense.Core.LicenseValidation;

// Load public key (embedded or from file)
var publicKey = File.ReadAllText("public_key.pem");

// Load license
var licenseJson = File.ReadAllText("license.json");

// Verify signature
var verifier = new LicenseVerifier(publicKey);
if (!verifier.VerifyLicenseDocumentJson(licenseJson, out var reason))
{
    throw new InvalidOperationException($"Invalid license: {reason}");
}

// Parse and use license data
var license = License.FromJson(licenseJson);
var maxUsers = (int)license["MaxUsers"];
var expiry = DateTime.Parse((string)license["ExpiryUtc"]);

if (DateTime.UtcNow > expiry)
{
    throw new InvalidOperationException("License has expired");
}
```

### Shell Script

```bash
#!/bin/bash
# validate_license.sh

LICENSE_FILE="license.json"
PUBLIC_KEY="public_key.pem"
SCHEMA="schema.yaml"

if ! slic validate-license -l "$LICENSE_FILE" -k "$PUBLIC_KEY" -s "$SCHEMA"; then
    echo "ERROR: License validation failed!"
    exit 1
fi

echo "License is valid!"
```

## More Information

- **Main Project:** [SimpleLicense README](../../../README.md)
- **Core Library:** [SimpleLicense.Core](../Core/README.md)
- **Examples:** [examples/](../../../examples/)
- **Syntax Comparison:** [docs/CLI_SYNTAX_COMPARISON.md](../../../docs/CLI_SYNTAX_COMPARISON.md)

## License

See [LICENSE](../../../LICENSE) file in the project root.
