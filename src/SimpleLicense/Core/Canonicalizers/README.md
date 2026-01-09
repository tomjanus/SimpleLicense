# SimpleLicense.Core.Canonicalizers

## Overview

The **SimpleLicense.Core.Canonicalizers** module provides file content canonicalization for license verification. Canonicalization normalizes file content to produce deterministic, stable representations that can be reliably hashed and verified across different systems and formats.

## What is Canonicalization?

Canonicalization transforms file content into a standardized form by:
- Normalizing line endings
- Removing comments and non-semantic whitespace
- Standardizing formatting
- Preserving structural elements essential to meaning

This ensures that semantically identical files produce the same hash, even if they differ in formatting, comments, or platform-specific line endings.

## Architecture

All canonicalizers implement the `IFileCanonicalizer` interface:

```csharp
public interface IFileCanonicalizer
{
    string Canonicalize(string input);
    IEnumerable<string> SupportedExtensions { get; }
}
```

## Available Canonicalizers

### 1. GenericTextFileCanonicalizer

A conservative, format-agnostic canonicalizer for general text files.

**Supported Extensions (default):** `.txt`, `.md`, `.csv`, `.log`, `.ini`, `.conf`

**Transformations:**
- Normalizes all line endings to `\n`
- Removes trailing whitespace
- Collapses runs of internal whitespace to single spaces
- **Preserves leading indentation** (important for Python, YAML, etc.)
- Strips full-line comments starting with `#`, `;`, or `//`
- Removes empty lines
- Ensures output ends with a single trailing newline

**Usage:**

```csharp
using SimpleLicense.Core.Canonicalizers;

// Use default extensions
var canonicalizer = new GenericTextFileCanonicalizer();

// Or specify custom extensions
var customCanonicalizer = new GenericTextFileCanonicalizer(
    new[] { ".txt", ".log", ".config" }
);

string input = @"# This is a comment
Line 1    with  extra   spaces
    Indented line

Line 2  ";

string canonical = canonicalizer.Canonicalize(input);
// Output:
// Line 1 with extra spaces
//     Indented line
// Line 2
```

**Use Cases:**
- Configuration files
- Log files
- Markdown documentation
- CSV data files
- Any plain text where indentation matters

### 2. InpFileCanonicalizer

Specialized canonicalizer for EPANET `.inp` files (hydraulic network simulation format).

**Supported Extensions:** `.inp`

**Transformations:**
- Normalizes line endings to `\n`
- Removes the entire `[TITLE]` section
- Strips comments (`;` prefix, both full-line and inline)
- Uppercases section headers (e.g., `[junctions]` → `[JUNCTIONS]`)
- Collapses all whitespace to single spaces
- Trims lines and removes empty lines
- Ensures output ends with a single trailing newline

**Usage:**

```csharp
using SimpleLicense.Core.Canonicalizers;

var inpCanonicalizer = new InpFileCanonicalizer();

string inpContent = @"[TITLE]
My Network Simulation
Created on 2026-01-05

[JUNCTIONS]
;ID    Elev    Demand
J1     100     50  ; Main junction
J2     110     75

[PIPES]
;ID    Node1   Node2   Length
P1     J1      J2      1000
";

string canonical = inpCanonicalizer.Canonicalize(inpContent);
// Output:
// [JUNCTIONS]
// J1 100 50
// J2 110 75
// [PIPES]
// P1 J1 J2 1000
```

**Use Cases:**
- EPANET hydraulic simulation files
- Water distribution network models
- Infrastructure modeling files

## Integration with SimpleLicense

Canonicalizers are registered with the `CanonicalizerRegistry` and used during license creation and verification:

```csharp
using SimpleLicense.Core;
using SimpleLicense.Core.Canonicalizers;
using System.Text;

// Register canonicalizers
var registry = new CanonicalizerRegistry();
registry.Register(new InpFileCanonicalizer());
registry.Register(new GenericTextFileCanonicalizer());

// Create a license with file hashing
var licenseCreator = new LicenseCreator(registry);
licenseCreator.AddFile("network.inp", Encoding.UTF8);
licenseCreator.AddFile("config.txt", Encoding.UTF8);

var license = licenseCreator.CreateLicense(Encoding.UTF8);

// Verification automatically uses registered canonicalizers
var verifier = new LicenseVerifier(registry);
bool isValid = verifier.Verify(license, publicKeyPath);
```

## Creating Custom Canonicalizers

To create a custom canonicalizer for your file format:

```csharp
using SimpleLicense.Core;

public class XmlFileCanonicalizer : IFileCanonicalizer
{
    public IEnumerable<string> SupportedExtensions => new[] { ".xml" };

    public string Canonicalize(string input)
    {
        // Your normalization logic:
        // - Remove XML comments
        // - Normalize attribute order
        // - Remove whitespace between tags
        // - Standardize formatting
        
        return normalizedXml;
    }
}

// Register and use
registry.Register(new XmlFileCanonicalizer());
```

## Best Practices

1. **Choose the Right Canonicalizer:**
   - Use specialized canonicalizers (like `InpFileCanonicalizer`) for known formats
   - Use `GenericTextFileCanonicalizer` as a fallback for unknown text files
   - Binary files should not be canonicalized

2. **Test Your Canonicalizers:**
   - Verify that semantically identical files produce identical output
   - Ensure essential structural information is preserved
   - Test with different line endings and encoding formats

3. **Preserve Meaning:**
   - Only normalize formatting that doesn't affect interpretation
   - Preserve indentation in indentation-sensitive formats
   - Keep structural elements intact

4. **Document Transformations:**
   - Clearly document what normalization rules are applied
   - Specify which elements are stripped vs. preserved
   - Provide examples of before/after transformations

## Thread Safety

All canonicalizers in this module are thread-safe and can be reused across multiple operations without synchronization.

## Performance Considerations

- Canonicalizers use compiled regular expressions for efficiency
- Large files are processed line-by-line to minimize memory overhead
- Extension lookups use case-insensitive hash sets for O(1) performance

## See Also

- [CanonicalizerRegistry](../Core/CanonicalizerRegistry.cs) - Registry for managing canonicalizers
- [LicenseCreator](../Core/LicenseCreator.cs) - Uses canonicalizers during license creation
- [LicenseVerifier](../Core/LicenseVerifier.cs) - Uses canonicalizers during verification
