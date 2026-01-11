# SimpleLicense Cryptography Module

This module provides cryptographic functionality for generating RSA key pairs, signing licenses, and verifying signatures. It ensures license integrity and authenticity through digital signatures.

## Overview

The cryptography system consists of three main components:

1. **KeyGenerator** - Generates RSA key pairs and saves them as PEM files
2. **LicenseSigner** - Signs licenses using RSA private keys
3. **LicenseVerifier** - Verifies license signatures using RSA public keys

## Security Features

- 🔐 **RSA Encryption** - Industry-standard RSA algorithm (2048/3072/4096-bit)
- 🔒 **SHA256 Hashing** - Secure hash algorithm for data integrity
- ✍️ **Digital Signatures** - Tamper-evident cryptographic signatures
- 📄 **PEM Format** - Standard PEM encoding for keys
- 🛡️ **Padding Options** - PSS (recommended) and PKCS1 padding support

## Components

### KeyGenerator

Generates RSA key pairs and saves them as PEM files.

**Location:** `KeyFilesGenerator.cs`

#### Basic Usage

```csharp
using SimpleLicense.Core.Cryptography;

// Create generator with 2048-bit keys (default)
var keyGen = new KeyGenerator(keySize: 2048);

// Optional: Set output directory
keyGen.KeyDir = new DirectoryInfo("./keys");

// Optional: Subscribe to info events
keyGen.OnInfo += msg => Console.WriteLine(msg);

// Generate keys
var result = keyGen.GenerateKeys();

if (result.Success)
{
    Console.WriteLine($"Private key: {result.PrivateKeyPath}");
    Console.WriteLine($"Public key: {result.PublicKeyPath}");
}
```

#### Key Sizes

```csharp
// 2048-bit (minimum, good for most uses)
var keyGen2048 = new KeyGenerator(2048);

// 3072-bit (recommended for high security)
var keyGen3072 = new KeyGenerator(3072);

// 4096-bit (maximum security, slower performance)
var keyGen4096 = new KeyGenerator(4096);
```

#### Custom Key Names

```csharp
var keyGen = new KeyGenerator(2048);
keyGen.KeyDir = new DirectoryInfo("./keys");

// Generate with custom name
var result = keyGen.GenerateKeys("my_product_keys");

// Output files:
// ./keys/my_product_keys_private.pem
// ./keys/my_product_keys_public.pem
```

#### Default Naming

If no custom name is provided, keys are named with size and date:

```
key_2048bit_20260111_private.pem
key_2048bit_20260111_public.pem
```

### LicenseSigner

Signs licenses using RSA private keys to ensure integrity and authenticity.

**Location:** `../LicenseSigner.cs` (Core directory)

#### Basic Usage

```csharp
using SimpleLicense.Core;

// Load private key from file
var privateKey = File.ReadAllText("private_key.pem");

// Create signer
var signer = new LicenseSigner(privateKey);

// Create license
var license = new License();
license["LicenseId"] = "ABC-123";
license["CustomerName"] = "Acme Corp";
license["MaxUsers"] = 100;

// Sign license (adds Signature field)
var signedLicense = signer.SignLicenseDocument(license);

// Signature is now in the license
var signature = signedLicense["Signature"];
Console.WriteLine($"Signature: {signature}");
```

#### Padding Options

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

#### Custom Serializer

```csharp
// Use custom serializer for signing
var serializer = new CanonicalLicenseSerializer
{
    UnSerializedFields = new List<string> { "Signature", "Metadata" }
};

var signer = new LicenseSigner(
    privateKey,
    padding: PaddingChoice.Pss,
    serializer: serializer
);
```

#### How Signing Works

1. **Canonicalization** - License is serialized to canonical JSON (sorted keys, no whitespace)
2. **Hashing** - Canonical JSON is hashed with SHA256
3. **Signing** - Hash is encrypted with private key using RSA
4. **Encoding** - Signature is Base64-encoded and stored in `Signature` field

```
License → Canonical JSON → SHA256 Hash → RSA Sign → Base64 → Signature Field
```

### LicenseVerifier

Verifies license signatures using RSA public keys to detect tampering.

**Location:** `../LicenseVerifier.cs` (Core directory)

#### Basic Usage

```csharp
using SimpleLicense.Core;

// Load public key from file
var publicKey = File.ReadAllText("public_key.pem");

// Create verifier
var verifier = new LicenseVerifier(publicKey);

// Load license
var licenseJson = File.ReadAllText("license.json");

// Verify signature
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

#### Verify License Object

```csharp
// If you already have a License object
var license = License.FromJson(licenseJson);

bool isValid = verifier.VerifyLicenseDocument(
    license, 
    out string? failureReason
);
```

#### Padding Compatibility

Verifier must use the same padding as signer:

```csharp
// Match signer's padding
var verifier = new LicenseVerifier(
    publicKey,
    padding: PaddingChoice.Pss  // Must match signer
);
```

#### Failure Reasons

The `failureReason` output parameter provides detailed error information:

```csharp
if (!verifier.VerifyLicenseDocumentJson(licenseJson, out var reason))
{
    // Possible reasons:
    // - "Signature missing or empty"
    // - "Signature is not valid Base64"
    // - "Signature verification failed" (tampered)
    // - "Invalid JSON: ..."
    // - "RSA verification error: ..."
    Console.WriteLine($"Failed: {reason}");
}
```

## Complete Workflow

### 1. Generate Keys (Once)

```csharp
// Generate RSA key pair (do this once for your product)
var keyGen = new KeyGenerator(keySize: 2048);
keyGen.KeyDir = new DirectoryInfo("./keys");
var result = keyGen.GenerateKeys();

// Keep private key SECURE (never distribute!)
// Distribute public key with your application
```

### 2. Sign License (Server/Build Process)

```csharp
// Load private key (keep secure!)
var privateKey = File.ReadAllText("./keys/private_key.pem");

// Create license for customer
var license = new License();
license["LicenseId"] = Guid.NewGuid().ToString();
license["CustomerName"] = "Acme Corporation";
license["MaxUsers"] = 100;
license["ExpiryUtc"] = DateTime.UtcNow.AddYears(1).ToString("O");

// Sign license
var signer = new LicenseSigner(privateKey);
var signedLicense = signer.SignLicenseDocument(license);

// Save and distribute to customer
var serializer = new LicenseSerializer { Options = JsonProfiles.Pretty };
var licenseJson = serializer.SerializeLicenseDocument(signedLicense);
File.WriteAllText("customer_license.json", licenseJson);
```

### 3. Verify License (Application Startup)

```csharp
// Load public key (embedded in application or read from file)
var publicKey = File.ReadAllText("./keys/public_key.pem");
// Or embed in code:
// var publicKey = @"-----BEGIN PUBLIC KEY-----
// MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA...
// -----END PUBLIC KEY-----";

// Load customer's license file
var licenseJson = File.ReadAllText("license.json");

// Verify signature
var verifier = new LicenseVerifier(publicKey);
if (!verifier.VerifyLicenseDocumentJson(licenseJson, out var reason))
{
    throw new UnauthorizedAccessException($"Invalid license: {reason}");
}

// Parse and use license data
var license = License.FromJson(licenseJson);
var maxUsers = (int)license["MaxUsers"];
var expiry = DateTime.Parse((string)license["ExpiryUtc"]);

// Check expiration
if (DateTime.UtcNow > expiry)
{
    throw new UnauthorizedAccessException("License has expired");
}

// Application is authorized!
Console.WriteLine($"Licensed to: {license["CustomerName"]}");
Console.WriteLine($"Max users: {maxUsers}");
```

## Security Best Practices

### Key Management

✅ **DO:**
- Generate keys on a secure machine
- Keep private keys in secure storage (hardware security module, encrypted vault)
- Use strong key sizes (2048-bit minimum, 3072+ recommended)
- Rotate keys periodically (every 2-3 years)
- Back up private keys securely

❌ **DON'T:**
- Never commit private keys to version control
- Never distribute private keys to customers
- Never embed private keys in applications
- Never share private keys via email or unsecured channels

### Application Security

```csharp
// ✅ Embed public key in application
const string PublicKey = @"-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA...
-----END PUBLIC KEY-----";

// ✅ Verify license on every application start
public static void ValidateLicense()
{
    var verifier = new LicenseVerifier(PublicKey);
    var licenseJson = File.ReadAllText("license.json");
    
    if (!verifier.VerifyLicenseDocumentJson(licenseJson, out var reason))
    {
        throw new UnauthorizedAccessException(reason);
    }
}

// ✅ Check license before sensitive operations
public void PerformPremiumFeature()
{
    if (!IsFeatureEnabled("PremiumFeature"))
    {
        throw new UnauthorizedAccessException("Feature not licensed");
    }
    // ... perform operation
}
```

### Tamper Detection

Signatures detect ANY modification to signed fields:

```csharp
// Original license
var license = new License();
license["MaxUsers"] = 50;
var signedLicense = signer.SignLicenseDocument(license);

// Someone tries to modify it
signedLicense["MaxUsers"] = 999;

// Verification fails!
bool isValid = verifier.VerifyLicenseDocument(signedLicense, out var reason);
// isValid = false
// reason = "Signature verification failed"
```

## Advanced Topics

### Canonical Serialization

Signatures are computed on canonical JSON to ensure consistency:

```csharp
var serializer = new CanonicalLicenseSerializer();

// Canonical format:
// - Sorted keys alphabetically
// - No whitespace
// - Consistent encoding
var canonical = serializer.SerializeLicenseDocument(license);

// Example output:
// {"CustomerName":"Acme","LicenseId":"ABC-123","MaxUsers":100}
```

### Excluding Fields from Signature

Some fields should not be signed (metadata, signature itself):

```csharp
var serializer = new CanonicalLicenseSerializer
{
    UnSerializedFields = new List<string> 
    { 
        "Signature",        // Never sign the signature field
        "Metadata",         // Can be modified without invalidating
        "LastCheckedUtc"    // Client-side tracking field
    }
};

var signer = new LicenseSigner(privateKey, serializer: serializer);
```

### Key Size Performance

| Key Size | Security Level | Sign Time | Verify Time | Recommendation |
|----------|---------------|-----------|-------------|----------------|
| 2048-bit | Good          | ~10ms     | ~1ms        | Minimum        |
| 3072-bit | Better        | ~30ms     | ~2ms        | Recommended    |
| 4096-bit | Best          | ~100ms    | ~5ms        | High Security  |

Choose based on your security requirements and performance constraints.

### Testing Signatures

```csharp
// Generate test keys
var keyGen = new KeyGenerator(2048);
var result = keyGen.GenerateKeys("test_keys");

// Sign test license
var privateKey = File.ReadAllText(result.PrivateKeyPath);
var signer = new LicenseSigner(privateKey);
var signedLicense = signer.SignLicenseDocument(testLicense);

// Verify immediately
var publicKey = File.ReadAllText(result.PublicKeyPath);
var verifier = new LicenseVerifier(publicKey);
Assert.IsTrue(verifier.VerifyLicenseDocument(signedLicense, out _));

// Test tampering detection
signedLicense["MaxUsers"] = 999;
Assert.IsFalse(verifier.VerifyLicenseDocument(signedLicense, out var reason));
Assert.AreEqual("Signature verification failed", reason);
```

## Error Handling

### Common Exceptions

```csharp
try
{
    var signer = new LicenseSigner(privateKey);
    var signed = signer.SignLicenseDocument(license);
}
catch (ArgumentNullException ex)
{
    // Private key or license is null
}
catch (CryptographicException ex)
{
    // Invalid key format or RSA operation failed
}
catch (Exception ex)
{
    // Other serialization or signing errors
}
```

### Verification Error Handling

```csharp
public void ValidateLicenseWithLogging(string licenseJson)
{
    var verifier = new LicenseVerifier(publicKey);
    
    if (!verifier.VerifyLicenseDocumentJson(licenseJson, out var reason))
    {
        _logger.LogError($"License verification failed: {reason}");
        
        // Different actions based on failure type
        if (reason.Contains("Signature missing"))
        {
            throw new InvalidLicenseException("License is not signed");
        }
        else if (reason.Contains("verification failed"))
        {
            throw new TamperedLicenseException("License has been modified");
        }
        else if (reason.Contains("Invalid JSON"))
        {
            throw new CorruptedLicenseException("License file is corrupted");
        }
        else
        {
            throw new UnauthorizedAccessException($"Invalid license: {reason}");
        }
    }
}
```

## Integration Examples

### ASP.NET Core Application

```csharp
// Startup.cs or Program.cs
public class LicenseValidator
{
    private readonly LicenseVerifier _verifier;
    private readonly string _licenseFilePath;
    
    public LicenseValidator(IConfiguration config)
    {
        var publicKey = config["License:PublicKey"];
        _verifier = new LicenseVerifier(publicKey);
        _licenseFilePath = config["License:FilePath"];
    }
    
    public void ValidateOnStartup()
    {
        if (!File.Exists(_licenseFilePath))
            throw new FileNotFoundException("License file not found");
            
        var licenseJson = File.ReadAllText(_licenseFilePath);
        
        if (!_verifier.VerifyLicenseDocumentJson(licenseJson, out var reason))
            throw new UnauthorizedAccessException($"Invalid license: {reason}");
            
        var license = License.FromJson(licenseJson);
        ValidateExpiration(license);
    }
}
```

### Desktop Application

```csharp
// App startup
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);
    
    try
    {
        ValidateLicense();
    }
    catch (UnauthorizedAccessException ex)
    {
        MessageBox.Show(
            $"License validation failed:\n{ex.Message}\n\nPlease contact support.",
            "License Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error
        );
        Shutdown(1);
    }
}
```

## Related Components

- **[LicenseCreator](../LicenseCreator.cs)** - Creates licenses from schemas
- **[LicenseValidator](../LicenseValidation/)** - Validates license structure
- **[CanonicalLicenseSerializer](../CanonicalLicenseSerializer.cs)** - Canonical JSON serialization
- **[Examples](../../../../examples/)** - Example code demonstrating usage

## References

- **RSA Algorithm:** [RFC 8017](https://tools.ietf.org/html/rfc8017)
- **PEM Format:** [RFC 7468](https://tools.ietf.org/html/rfc7468)
- **SHA-256:** [FIPS 180-4](https://csrc.nist.gov/publications/detail/fips/180/4/final)

## License

See [LICENSE](../../../../../LICENSE) file in the project root.
