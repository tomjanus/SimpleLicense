# SimpleLicense.Core.Utils

## Overview

The **SimpleLicense.Core.Utils** namespace provides utility classes and helper functions for the SimpleLicense library. These utilities handle common tasks like encoding resolution, type checking, and data validation.

## Available Utilities

### 1. TextFileHasher

A static utility class for computing SHA-256 hashes of strings and text files with optional canonicalization.

**Purpose:** Provides cryptographic hashing for license validation, file integrity checking, and tamper detection.

#### Methods

##### Sha256Hex(string input, Encoding? encoding = null)

Computes SHA-256 hash of an input string and returns it as a lowercase hexadecimal string.

**Parameters:**
- `input` - The string to hash (required)
- `encoding` - Text encoding to use (default: UTF-8)

**Returns:** Lowercase hex string representing the SHA-256 hash

**Throws:** `ArgumentNullException` if `input` is null

**Usage:**

```csharp
using SimpleLicense.Core.Utils;
using System.Text;

// Basic hashing
string hash = TextFileHasher.Sha256Hex("Hello, World!");
Console.WriteLine(hash); // Output: lowercase hex string (64 characters)

// With specific encoding
string hash2 = TextFileHasher.Sha256Hex("Data", Encoding.UTF8);

// For license validation
string licenseKey = "ABC-123-XYZ";
string keyHash = TextFileHasher.Sha256Hex(licenseKey);
```

##### HashFile(string path, IFileCanonicalizer? canonicalizer = null, string encodingName = "utf-8")

Computes SHA-256 hash of a text file with optional canonicalization before hashing.

**Parameters:**
- `path` - Path to the text file to hash (required)
- `canonicalizer` - Optional canonicalizer to normalize file content before hashing
- `encodingName` - Name of text encoding (default: "utf-8")

**Returns:** Lowercase hex string representing the SHA-256 hash of the file content

**Throws:** 
- `ArgumentNullException` if `path` is null
- `ArgumentException` if encoding name is not supported
- `FileNotFoundException` if file doesn't exist

**Usage:**

```csharp
using SimpleLicense.Core.Utils;
using SimpleLicense.Core.Canonicalizers;

// Basic file hashing
string fileHash = TextFileHasher.HashFile("config.txt");

// With specific encoding
string hash2 = TextFileHasher.HashFile("data.txt", encodingName: "utf-16");

// With canonicalization (removes comments, normalizes whitespace)
var canonicalizer = new GenericTextFileCanonicalizer();
string canonicalHash = TextFileHasher.HashFile(
    "config.yaml",
    canonicalizer: canonicalizer
);

// For file protection licenses
string appHash = TextFileHasher.HashFile("app.exe");
string configHash = TextFileHasher.HashFile("config.xml", canonicalizer);
```

**Common Use Cases:**

**1. File integrity checking:**
```csharp
// Store hash in license
string originalHash = TextFileHasher.HashFile("app.exe");
license["AppHash"] = originalHash;

// Later, verify file hasn't changed
string currentHash = TextFileHasher.HashFile("app.exe");
if (currentHash != license["AppHash"])
{
    throw new SecurityException("Application file has been modified!");
}
```

**2. License-protected configuration files:**
```csharp
var canonicalizer = new GenericTextFileCanonicalizer();
string configHash = TextFileHasher.HashFile(
    "config.yaml",
    canonicalizer: canonicalizer,
    encodingName: "utf-8"
);

// Users can modify comments and whitespace without breaking validation
// because canonicalization removes these before hashing
```

### 2. FileIO and IFileIO

File I/O abstraction with configurable text encoding support for reading and writing files.

**Purpose:** Provides a consistent interface for file operations with customizable default encoding, enabling dependency injection and testing.

#### IFileIO Interface

Defines contracts for common file operations including synchronous and asynchronous methods.

**Methods:**
- `string ReadAllText(string path)`
- `Task<string> ReadAllTextAsync(string path, CancellationToken ct = default)`
- `void WriteAllText(string path, string content)`
- `Task WriteAllTextAsync(string path, string content, CancellationToken ct = default)`
- `byte[] ReadAllBytes(string path)`
- `Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct = default)`
- `void WriteAllBytes(string path, byte[] data)`
- `Task WriteAllBytesAsync(string path, byte[] data, CancellationToken ct = default)`
- `bool FileExists(string path)`
- `void CreateDirectory(string path)`

#### FileIO Class

Concrete implementation that wraps standard `System.IO.File` and `System.IO.Directory` operations.

**Constructor:**
```csharp
public FileIO(Encoding? defaultEncoding = null)
```

**Properties:**
- `DefaultEncoding` - The encoding used for all text operations (default: UTF-8)

**Usage:**

```csharp
using SimpleLicense.Core.Utils;
using System.Text;

// Default UTF-8 encoding
var fileIO = new FileIO();
string content = fileIO.ReadAllText("config.txt");

// Custom encoding
var fileIOUtf16 = new FileIO(Encoding.Unicode);
string utf16Content = fileIOUtf16.ReadAllText("data.txt");

// Async operations
var fileIO = new FileIO();
string content = await fileIO.ReadAllTextAsync("license.json");

// Write operations
fileIO.WriteAllText("output.txt", "License data");

// Binary operations
byte[] data = fileIO.ReadAllBytes("signature.bin");
fileIO.WriteAllBytes("copy.bin", data);

// Directory operations
if (!fileIO.FileExists("config.txt"))
{
    fileIO.CreateDirectory("./configs");
    fileIO.WriteAllText("./configs/config.txt", "default");
}
```

**Dependency Injection:**

```csharp
public class LicenseManager
{
    private readonly IFileIO _fileIO;
    
    public LicenseManager(IFileIO fileIO)
    {
        _fileIO = fileIO;
    }
    
    public string LoadLicense(string path)
    {
        if (!_fileIO.FileExists(path))
            throw new FileNotFoundException($"License not found: {path}");
        
        return _fileIO.ReadAllText(path);
    }
}

// Usage
var fileIO = new FileIO(Encoding.UTF8);
var manager = new LicenseManager(fileIO);
```

**Testing with Mock:**

```csharp
// Mock implementation for testing
public class MockFileIO : IFileIO
{
    private Dictionary<string, string> _files = new();
    
    public string ReadAllText(string path) => 
        _files.TryGetValue(path, out var content) ? content : 
        throw new FileNotFoundException();
    
    public void WriteAllText(string path, string content) => 
        _files[path] = content;
    
    // ... implement other methods
}

// Test
var mockIO = new MockFileIO();
mockIO.WriteAllText("test.txt", "test content");
var manager = new LicenseManager(mockIO);
```

### 3. File Source Abstractions

Abstractions for enumerating files from different sources (folders, lists, etc.).

**Purpose:** Provides flexible file enumeration for batch processing operations like hashing multiple files.

#### IFileSource Interface

Abstraction for a source of files to be processed.

**Methods:**
- `IEnumerable<string> EnumerateFiles()` - Returns full file paths

#### FolderFileSource

File source that reads from a specified folder with optional pattern matching and recursion.

**Constructor:**
```csharp
public FolderFileSource(
    string folder,
    string pattern = "*.*",
    bool recursive = true
)
```

**Parameters:**
- `folder` - The folder to read files from (required)
- `pattern` - Search pattern (e.g., "*.txt", "*.json") - default: "*.*"
- `recursive` - Whether to search subdirectories - default: true

**Throws:**
- `ArgumentNullException` if `folder` is null
- `DirectoryNotFoundException` if folder doesn't exist
- `ArgumentException` if pattern is null or whitespace

**Usage:**

```csharp
using SimpleLicense.Core.Utils;

// Enumerate all files recursively
var source = new FolderFileSource("./data");
foreach (var filePath in source.EnumerateFiles())
{
    Console.WriteLine(filePath);
}

// Only .txt files, top-level only
var txtSource = new FolderFileSource(
    folder: "./configs",
    pattern: "*.txt",
    recursive: false
);

// Hash all configuration files
var configSource = new FolderFileSource("./configs", "*.yaml");
foreach (var file in configSource.EnumerateFiles())
{
    string hash = TextFileHasher.HashFile(file);
    Console.WriteLine($"{file}: {hash}");
}
```

#### ListFileSource

File source that reads from a provided list of file paths.

**Constructor:**
```csharp
public ListFileSource(IEnumerable<string>? paths = null)
```

**Parameters:**
- `paths` - Collection of file paths (can be null or empty)

**Usage:**

```csharp
using SimpleLicense.Core.Utils;

// From explicit list
var paths = new[] { "file1.txt", "file2.txt", "file3.txt" };
var source = new ListFileSource(paths);

foreach (var filePath in source.EnumerateFiles())
{
    string hash = TextFileHasher.HashFile(filePath);
}

// From dynamic collection
var criticalFiles = Directory.GetFiles("./")
    .Where(f => f.Contains("config") || f.Contains("license"));
var fileSource = new ListFileSource(criticalFiles);

// Null-safe (returns empty enumerable)
var emptySource = new ListFileSource(null);
foreach (var file in emptySource.EnumerateFiles())
{
    // Won't execute
}
```

**Polymorphic Usage:**

```csharp
public void ProcessFiles(IFileSource source)
{
    foreach (var file in source.EnumerateFiles())
    {
        string hash = TextFileHasher.HashFile(file);
        Console.WriteLine($"{Path.GetFileName(file)}: {hash}");
    }
}

// Use with folder
ProcessFiles(new FolderFileSource("./data", "*.json"));

// Use with list
var importantFiles = new[] { "app.exe", "config.xml" };
ProcessFiles(new ListFileSource(importantFiles));
```

### 4. EncodingMap

A static utility class for mapping encoding names to .NET `Encoding` objects with case-insensitive lookup.

**Purpose:** Simplifies encoding selection by accepting user-friendly string names instead of requiring direct `Encoding` object construction.

#### Supported Encodings

| Encoding Name | .NET Encoding | Description |
|--------------|---------------|-------------|
| `utf-8` | `Encoding.UTF8` | Unicode 8-bit encoding (default) |
| `utf-16` | `Encoding.Unicode` | Unicode 16-bit little-endian |
| `utf-16be` | `Encoding.BigEndianUnicode` | Unicode 16-bit big-endian |
| `utf-32` | `Encoding.UTF32` | Unicode 32-bit encoding |
| `ascii` | `Encoding.ASCII` | 7-bit ASCII encoding |
| `us-ascii` | `Encoding.ASCII` | Alias for ASCII |
| `latin1` | `Encoding.Latin1` | ISO-8859-1 encoding |

#### Methods

##### GetEncoding(string name)

Gets an `Encoding` object from a string name. Throws an exception if the encoding is not supported.

**Parameters:**
- `name` - The encoding name (case-insensitive)

**Returns:** The corresponding `Encoding` object

**Throws:** 
- `ArgumentNullException` if `name` is null
- `ArgumentException` if the encoding name is not recognized

**Usage:**

```csharp
using SimpleLicense.Core.Utils;
using System.Text;

// Get encoding - throws exception if not found
Encoding utf8 = EncodingMap.GetEncoding("utf-8");
Encoding utf16 = EncodingMap.GetEncoding("UTF-16");  // Case-insensitive

// Use for file operations
string content = File.ReadAllText("file.txt", utf8);

// Example with error handling
try
{
    Encoding enc = EncodingMap.GetEncoding("unknown");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    // Output: Error: Unsupported encoding: unknown
}
```

##### TryGetEncoding(string name, out Encoding? encoding)

Attempts to get an `Encoding` object from a string name. Returns `false` if not found instead of throwing.

**Parameters:**
- `name` - The encoding name (case-insensitive)
- `encoding` - Output parameter that receives the encoding object (or null)

**Returns:** `true` if the encoding was found, `false` otherwise

**Usage:**

```csharp
using SimpleLicense.Core.Utils;
using System.Text;

// Try to get encoding - safe pattern
if (EncodingMap.TryGetEncoding("utf-8", out var encoding))
{
    // Success: encoding contains Encoding.UTF8
    byte[] data = encoding.GetBytes("Hello, World!");
}
else
{
    // Failed: encoding is null
    Console.WriteLine("Encoding not supported");
}

// Handle user input safely
string userInput = "latin1";
if (EncodingMap.TryGetEncoding(userInput, out var userEncoding))
{
    File.WriteAllText("output.txt", "Content", userEncoding);
}

// Null input handling
EncodingMap.TryGetEncoding(null, out var nullEnc);  // Returns false, nullEnc is null
```

#### Key Features

- **Case-insensitive lookup:** `"UTF-8"`, `"utf-8"`, and `"Utf-8"` all work
- **Thread-safe:** All operations are read-only on static collections
- **Fast lookups:** Uses `Dictionary` with `StringComparer.OrdinalIgnoreCase` for O(1) performance
- **Null-safe:** Properly handles null input in both methods

#### Common Use Cases

**1. CLI argument parsing:**
```csharp
string encodingArg = args[0];  // e.g., "utf-8"
if (EncodingMap.TryGetEncoding(encodingArg, out var encoding))
{
    ProcessFile("data.txt", encoding);
}
```

**2. Configuration file reading:**
```csharp
var config = LoadConfig();
Encoding fileEncoding = EncodingMap.GetEncoding(config.FileEncoding);
```

**3. Default with fallback:**
```csharp
Encoding encoding = EncodingMap.TryGetEncoding(userChoice, out var enc) 
    ? enc 
    : Encoding.UTF8;  // Default to UTF-8
```

### 5. TypeChecking

An internal static utility class for runtime type checking and type information extraction.

**Note:** This class is marked `internal` and is used by other SimpleLicense components. To use it externally, change the class access modifier to `public`.

#### Methods

##### IsNumeric(object? value, out double number)

Checks if a given object is of a numeric type and outputs its value as a `double`.

**Parameters:**
- `value` - The object to check
- `number` - Output parameter that receives the numeric value (or 0 if not numeric)

**Returns:** `true` if the value is numeric, `false` otherwise

**Supported Numeric Types:**
- `byte`, `sbyte`
- `short`, `ushort`
- `int`, `uint`
- `long`, `ulong`
- `float`
- `double`
- `decimal` (requires explicit cast)

**Usage:**

```csharp
using SimpleLicense.Core.Utils;

object value1 = 42;
if (TypeChecking.IsNumeric(value1, out double num))
{
    Console.WriteLine($"Numeric value: {num}");  // Output: 42
}

object value2 = "hello";
if (!TypeChecking.IsNumeric(value2, out double num2))
{
    Console.WriteLine("Not a numeric value");
}

// Works with all numeric types
object floatVal = 3.14f;
object decimalVal = 99.99m;
object longVal = 1000000L;

TypeChecking.IsNumeric(floatVal, out double f);    // true, f = 3.14
TypeChecking.IsNumeric(decimalVal, out double d);  // true, d = 99.99
TypeChecking.IsNumeric(longVal, out double l);     // true, l = 1000000
```

**Note on Type Conversions:**
- Most numeric types are **implicitly convertible** to `double`
- `decimal` requires **explicit casting** to `double` (may lose precision)
- Non-numeric values return `false` with `number = 0`

##### DescribeType(object? value)

Returns a human-readable string description of the object's type.

**Parameters:**
- `value` - The object to describe

**Returns:** The type name as a string, or `"null"` if the value is null

**Usage:**

```csharp
using SimpleLicense.Core.Utils;

object value1 = 42;
Console.WriteLine(TypeChecking.DescribeType(value1));  // Output: Int32

object value2 = "hello";
Console.WriteLine(TypeChecking.DescribeType(value2));  // Output: String

object? value3 = null;
Console.WriteLine(TypeChecking.DescribeType(value3));  // Output: null

List<int> list = new List<int>();
Console.WriteLine(TypeChecking.DescribeType(list));    // Output: List`1
```

**Common Use Cases:**

**1. Validation with type reporting:**
```csharp
object userInput = GetUserInput();
if (!TypeChecking.IsNumeric(userInput, out double val))
{
    string actualType = TypeChecking.DescribeType(userInput);
    throw new ValidationException(
        $"Expected numeric value, got {actualType}"
    );
}
```

**2. Dynamic type checking:**
```csharp
void ProcessValue(object value)
{
    if (TypeChecking.IsNumeric(value, out double num))
    {
        Console.WriteLine($"Processing number: {num}");
    }
    else
    {
        Console.WriteLine($"Cannot process {TypeChecking.DescribeType(value)}");
    }
}
```

**3. Debugging and logging:**
```csharp
object data = GetData();
Logger.Debug($"Received data of type: {TypeChecking.DescribeType(data)}");
```

## Design Patterns

### TextFileHasher

- **Static utility class:** No instantiation needed
- **SHA-256 algorithm:** Industry-standard cryptographic hash function
- **Canonicalization support:** Optional normalization before hashing for consistent results
- **Encoding-aware:** Handles different text encodings correctly

### FileIO / IFileIO

- **Interface segregation:** Clean abstraction for file operations
- **Dependency injection:** Enables testing and flexibility
- **Encoding configuration:** Consistent encoding across all text operations
- **Async support:** Non-blocking I/O operations

### File Source Abstractions

- **Strategy pattern:** Interchangeable file enumeration sources
- **Lazy evaluation:** Files enumerated on-demand using `yield return`
- **Null-safe:** Handles empty/null collections gracefully

### EncodingMap

- **Static utility class:** No instantiation needed
- **Dictionary-based lookup:** Fast O(1) access
- **Two methods pattern:** `Get` (throws) + `Try` (safe) for different error handling needs

### TypeChecking

- **Pattern matching with switch expressions:** Modern C# type checking
- **Out parameters:** Returns both success status and extracted value
- **Null-safe:** Handles null input gracefully

## Thread Safety

All utilities in this namespace are **thread-safe**:
- `TextFileHasher` uses stateless static methods (thread-safe by design)
- `EncodingMap` uses read-only static collections
- `TypeChecking` has no mutable state
- `FileIO` instance state is read-only (DefaultEncoding)
- `IFileSource` implementations use no shared mutable state

## Performance Considerations

- **TextFileHasher:** SHA-256 is optimized in .NET using hardware acceleration where available
- **FileIO:** Wraps standard .NET file operations with minimal overhead
- **EncodingMap:** O(1) lookups using hash-based dictionary
- **TypeChecking:** O(1) pattern matching with switch expressions
- **File Sources:** Lazy enumeration - files discovered on-demand, not all at once
- **No allocations:** Minimal memory overhead for all utilities

## Best Practices

1. **File Hashing:**
   - Use canonicalization for config files to ignore formatting changes
   - Always specify encoding explicitly for cross-platform consistency
   - Store hashes in licenses to detect file tampering
   - Re-hash files at application startup for validation

2. **File I/O:**
   - Use `IFileIO` interface for dependency injection
   - Configure default encoding once at application startup
   - Use async methods for large files or network storage
   - Check `FileExists` before operations to avoid exceptions

3. **File Sources:**
   - Use `FolderFileSource` for directory-based processing
   - Use `ListFileSource` when you have explicit file lists
   - Implement `IFileSource` for custom enumeration logic
   - Handle empty enumerations gracefully

4. **Encoding Selection:**
   - Use `TryGetEncoding` when handling user input
   - Use `GetEncoding` when encoding name is guaranteed valid
   - Default to UTF-8 for cross-platform compatibility

5. **Type Checking:**
   - Use `IsNumeric` before numeric operations on dynamic types
   - Use `DescribeType` for error messages and logging
   - Be aware of precision loss when converting `decimal` to `double`

6. **Error Handling:**
   - Validate file paths before hashing
   - Handle encoding errors appropriately
   - Provide meaningful error messages using `DescribeType`
   - Catch `FileNotFoundException` and `DirectoryNotFoundException`

## Examples

### Complete File Processing Example

```csharp
using SimpleLicense.Core.Utils;
using SimpleLicense.Core.Canonicalizers;
using System.Text;

public class LicenseFileProtector
{
    private readonly IFileIO _fileIO;
    
    public LicenseFileProtector(IFileIO? fileIO = null)
    {
        _fileIO = fileIO ?? new FileIO(Encoding.UTF8);
    }
    
    /// <summary>
    /// Creates a license with file protection hashes
    /// </summary>
    public Dictionary<string, string> GenerateFileHashes(IFileSource fileSource)
    {
        var hashes = new Dictionary<string, string>();
        var canonicalizer = new GenericTextFileCanonicalizer();
        
        foreach (var filePath in fileSource.EnumerateFiles())
        {
            // Hash with canonicalization for text files
            string hash = TextFileHasher.HashFile(
                filePath,
                canonicalizer: canonicalizer,
                encodingName: "utf-8"
            );
            
            string fileName = Path.GetFileName(filePath);
            hashes[fileName] = hash;
        }
        
        return hashes;
    }
    
    /// <summary>
    /// Validates files against stored hashes
    /// </summary>
    public bool ValidateFileIntegrity(
        Dictionary<string, string> expectedHashes,
        string directory)
    {
        var canonicalizer = new GenericTextFileCanonicalizer();
        
        foreach (var (fileName, expectedHash) in expectedHashes)
        {
            string filePath = Path.Combine(directory, fileName);
            
            if (!_fileIO.FileExists(filePath))
            {
                Console.WriteLine($"Missing file: {fileName}");
                return false;
            }
            
            string actualHash = TextFileHasher.HashFile(
                filePath,
                canonicalizer: canonicalizer
            );
            
            if (actualHash != expectedHash)
            {
                Console.WriteLine($"File modified: {fileName}");
                return false;
            }
        }
        
        Console.WriteLine("✓ All files validated successfully");
        return true;
    }
}

// Usage
var protector = new LicenseFileProtector();

// Generate hashes for all config files
var configSource = new FolderFileSource("./configs", "*.yaml");
var hashes = protector.GenerateFileHashes(configSource);

// Store hashes in license
foreach (var (file, hash) in hashes)
{
    license[$"Hash_{file}"] = hash;
}

// Later, validate files
bool isValid = protector.ValidateFileIntegrity(hashes, "./configs");
```

### File Processing with Encoding

```csharp
using SimpleLicense.Core.Utils;
using System.Text;

public class FileProcessor
{
    public void ProcessFile(string filePath, string encodingName)
    {
        // Safe encoding resolution
        if (!EncodingMap.TryGetEncoding(encodingName, out var encoding))
        {
            throw new ArgumentException(
                $"Unsupported encoding: {encodingName}. " +
                "Supported: utf-8, utf-16, ascii, latin1"
            );
        }

        // Create FileIO with specified encoding
        var fileIO = new FileIO(encoding);
        
        // Read file with specified encoding
        string content = fileIO.ReadAllText(filePath);
        Console.WriteLine($"Read {content.Length} characters using {encodingName}");
        
        // Hash the content
        string hash = TextFileHasher.Sha256Hex(content, encoding);
        Console.WriteLine($"SHA-256: {hash}");
    }

    public void ValidateNumericField(string fieldName, object? value)
    {
        if (value == null)
        {
            throw new ValidationException($"{fieldName} cannot be null");
        }

        if (!TypeChecking.IsNumeric(value, out double numValue))
        {
            string actualType = TypeChecking.DescribeType(value);
            throw new ValidationException(
                $"{fieldName} must be numeric, got {actualType}"
            );
        }

        Console.WriteLine($"{fieldName} validated: {numValue}");
    }
}
```

### Batch File Hashing

```csharp
using SimpleLicense.Core.Utils;

public class BatchHasher
{
    public static Dictionary<string, string> HashAllFiles(
        string directory,
        string pattern = "*.*",
        bool recursive = true)
    {
        var results = new Dictionary<string, string>();
        var source = new FolderFileSource(directory, pattern, recursive);
        
        foreach (var filePath in source.EnumerateFiles())
        {
            try
            {
                string hash = TextFileHasher.HashFile(filePath);
                string relativePath = Path.GetRelativePath(directory, filePath);
                results[relativePath] = hash;
                Console.WriteLine($"✓ {relativePath}: {hash[..16]}...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ {filePath}: {ex.Message}");
            }
        }
        
        return results;
    }
}

// Usage
var hashes = BatchHasher.HashAllFiles("./release", "*.dll");
foreach (var (file, hash) in hashes)
{
    Console.WriteLine($"{file}: {hash}");
}
```

## Extension Points

### Adding Custom Encodings

To add support for additional encodings, modify `EncodingMap._encodings`:

```csharp
// In EncodingMap.cs
{ "iso-8859-1", Encoding.GetEncoding("ISO-8859-1") },
{ "windows-1252", Encoding.GetEncoding(1252) },
{ "shift-jis", Encoding.GetEncoding("shift_jis") },
```

### Custom File Sources

Implement `IFileSource` for custom file enumeration:

```csharp
public class DatabaseFileSource : IFileSource
{
    private readonly string _connectionString;
    
    public DatabaseFileSource(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public IEnumerable<string> EnumerateFiles()
    {
        // Query database for file paths
        using var connection = new SqlConnection(_connectionString);
        var command = new SqlCommand("SELECT FilePath FROM Files", connection);
        connection.Open();
        
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            yield return reader.GetString(0);
        }
    }
}
```

### Custom File I/O

Implement `IFileIO` for specialized storage:

```csharp
public class CloudFileIO : IFileIO
{
    private readonly CloudStorageClient _client;
    
    public CloudFileIO(CloudStorageClient client)
    {
        _client = client;
    }
    
    public string ReadAllText(string path)
    {
        var blob = _client.GetBlob(path);
        return blob.DownloadText();
    }
    
    // ... implement other methods
}
```

## See Also

- **[System.Text.Encoding](https://learn.microsoft.com/en-us/dotnet/api/system.text.encoding)** - .NET Encoding documentation
- **[System.Security.Cryptography](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography)** - .NET Cryptography documentation
- **[SimpleLicense.Core](../)** - Core library that uses these utilities
- **[Canonicalizers](../Canonicalizers/)** - Uses TextFileHasher for file integrity
- **[LicenseCreator](../LicenseCreator.cs)** - Uses file hashing for license generation
- **[Examples](../../../../examples/)** - Example code demonstrating usage
