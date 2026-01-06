# SimpleLicense.Utils

## Overview

The **SimpleLicense.Utils** namespace provides utility classes and helper functions for the SimpleLicense library. These utilities handle common tasks like encoding resolution, type checking, and data validation.

## Available Utilities

### 1. EncodingMap

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
using SimpleLicense.Utils;
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
using SimpleLicense.Utils;
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

### 2. TypeChecking

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
using SimpleLicense.Utils;

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
using SimpleLicense.Utils;

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
- `EncodingMap` uses read-only static collections
- `TypeChecking` has no mutable state

## Performance Considerations

- **EncodingMap:** O(1) lookups using hash-based dictionary
- **TypeChecking:** O(1) pattern matching with switch expressions
- **No allocations:** Minimal memory overhead for both utilities

## Best Practices

1. **Encoding Selection:**
   - Use `TryGetEncoding` when handling user input
   - Use `GetEncoding` when encoding name is guaranteed valid
   - Default to UTF-8 for cross-platform compatibility

2. **Type Checking:**
   - Use `IsNumeric` before numeric operations on dynamic types
   - Use `DescribeType` for error messages and logging
   - Be aware of precision loss when converting `decimal` to `double`

3. **Error Handling:**
   - Validate encoding names before processing files
   - Provide meaningful error messages using `DescribeType`

## Examples

### Complete File Processing Example

```csharp
using SimpleLicense.Utils;
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

        // Read file with specified encoding
        string content = File.ReadAllText(filePath, encoding);
        Console.WriteLine($"Read {content.Length} characters using {encodingName}");
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

## Extension Points

To add support for additional encodings:

```csharp
// Modify EncodingMap._encodings dictionary to include:
{ "iso-8859-1", Encoding.GetEncoding("ISO-8859-1") },
{ "windows-1252", Encoding.GetEncoding(1252) },
```

## See Also

- [System.Text.Encoding](https://learn.microsoft.com/en-us/dotnet/api/system.text.encoding) - .NET Encoding documentation
- [Core Module](../Core/README.md) - Uses these utilities for file operations
- [Canonicalizers](../Canonicalizers/README.md) - Uses EncodingMap for file reading
