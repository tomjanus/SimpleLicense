# License Management and Validation

## Overview

`SimpleLicense` provides a flexible system for creating, managing, and validating software licenses. At its core is the **`License`** class, which represents a license as a collection of named fields with values. This license data structure is passed to your application to control features, enforce limits, and manage license lifecycle.

The system also includes an optional **two-tier validation system** that provides field-level data validation (Tier 1) and schema-level structural validation (Tier 2). The field-level validation ensures that individual field values are correct and well-formed when they are set, while the schema-level validation checks that the overall license structure conforms to a defined schema, i.e. that required fields are present and of the correct type.

### What is a License?

A **`License`** is the central data structure in `SimpleLicense`. It represents a license as a flexible, field-based document where:
- Each field has a **name** (e.g., `"LicenseId"`, `"MaxUsers"`, `"ExpiryUtc"`)
- Each field has a **value** (e.g., `"LIC-2026-001"`, `100`, `DateTime`)
- Fields are case-insensitive (`"LicenseId"` == `"licenseid"`)
- Any field can be added dynamically
- There are three mandatory fields: `LicenseId`, `ExpiryUtc`, `Signature`. These fields are required for all licenses, because every license needs a unique identifier, an expiration date, and a signature. Other fields are optional and can be defined as needed
- The non-mandatory fields can be used to store custom data relevant to your application, such as user limits, feature flags, customer information, etc.

**Location:** [`../License.cs`](../License.cs)

### Creating a License

```csharp
using SimpleLicense.Core;
using SimpleLicense.Core.LicenseValidation;

// Create a new license
var license = new License();

// Set mandatory fields
license["LicenseId"] = "LIC-2026-001";
license["ExpiryUtc"] = DateTime.UtcNow.AddYears(1);
license["Signature"] = "ABC123XYZ...";

// Add custom fields for your application
license["CustomerName"] = "Acme Corporation";
license["MaxUsers"] = 100;
license["Features"] = new List<string> { "BasicFeatures", "AdvancedReporting" };
license["Tier"] = "Enterprise";

// Access fields
string customerId = (string)license["CustomerName"];
int maxUsers = (int)license["MaxUsers"];
DateTime expiry = (DateTime)license["ExpiryUtc"];
```

### Serialization - Persisting and Loading Licenses

Licenses can be serialized to/from JSON for storage and transmission:

```csharp
// Save license to JSON
string json = license.ToJson();
File.WriteAllText("license.json", json);

// Load license from JSON
string json = File.ReadAllText("license.json");
var license = License.FromJson(json);

// Validate mandatory fields are present
license.EnsureMandatoryPresent();
```

**Example License JSON:**
```json
{
  "LicenseId": "LIC-2026-001",
  "ExpiryUtc": "2027-12-31T23:59:59Z",
  "Signature": "ABC123XYZ...",
  "CustomerName": "Acme Corporation",
  "MaxUsers": 100,
  "Features": ["BasicFeatures", "AdvancedReporting"],
  "Tier": "Enterprise"
}
```

### Using License Data in Your Application

Once you have a `LicenseDocument`, your application uses it to control behavior:

```csharp
// Check if license is expired
var expiry = (DateTime)license["ExpiryUtc"];
if (DateTime.UtcNow > expiry)
{
    Console.WriteLine("License has expired!");
    return;
}

// Check user limits
int currentUsers = GetCurrentUserCount();
int maxUsers = license["MaxUsers"] as int? ?? int.MaxValue;
if (currentUsers >= maxUsers)
{
    Console.WriteLine($"User limit reached ({maxUsers} users)");
    return;
}

// Check feature access
var features = license["Features"] as List<object>;
bool hasAdvancedReporting = features?.Contains("AdvancedReporting") ?? false;
if (hasAdvancedReporting)
{
    ShowAdvancedReportingUI();
}

// Check tier
string tier = license["Tier"] as string ?? "Basic";
switch (tier)
{
    case "Enterprise":
        EnableEnterpriseFeatures();
        break;
    case "Professional":
        EnableProfessionalFeatures();
        break;
    default:
        EnableBasicFeatures();
        break;
}
```

### Mandatory Fields

Every license must have three mandatory fields:

| Field | Type | Purpose |
|-------|------|---------|
| `LicenseId` | string | Unique identifier for the license |
| `ExpiryUtc` | DateTime | Expiration date/time in UTC |
| `Signature` | string | Cryptographic signature (can be null for unsigned licenses) |

Use `EnsureMandatoryPresent()` to validate these fields:

```csharp
try
{
    license.EnsureMandatoryPresent();
    Console.WriteLine("✓ All mandatory fields present");
}
catch (LicenseValidationException ex)
{
    Console.WriteLine("✗ Missing or invalid mandatory fields:");
    foreach (var issue in ex.Issues)
    {
        Console.WriteLine($"  - {issue}");
    }
}
```

### Field Enumeration

Access all fields in a license:

```csharp
foreach (var field in license.Fields)
{
    Console.WriteLine($"{field.Key} = {field.Value}");
}
```

## Schema and Validation

While licenses are flexible and can contain any fields, you may want to enforce a specific structure with **schemas** (validating license document structure against a schema) and field **validation** (validating if fields have values that are allowed).

## Two-Tier Validation System

### Tier 1: Field-Level Validation (`FieldValidators.cs`)

**Purpose:** Validates individual field VALUES  
**When Applied:** When setting a field in a `License`  
**What It Checks:** Data quality, format, range, type conversion

**Example Validations:**
- `LicenseId` must be a non-empty string
- `ExpiryUtc` must be a valid datetime (supports multiple formats)
- `MaxUsers` must be a positive integer
- Field values are normalized (e.g., datetime strings → DateTime objects)

**Code Example:**
```csharp
var license = new License();
license["LicenseId"] = "LIC-2026-001";  // ✓ Valid
license["LicenseId"] = "";               // ✗ Throws exception: empty string
license["MaxUsers"] = 50;                // ✓ Valid
license["MaxUsers"] = "notanumber";      // ✗ Throws exception: not numeric
```

### Tier 2: Schema-Level Validation (`LicenseValidator.cs`)

**Purpose:** Validates license STRUCTURE against a schema  
**When Applied:** Explicitly by calling `LicenseValidator.Validate()`  
**What It Checks:** Structural compliance, required fields, type matching

**Example Validations:**
- All required fields defined in the schema are present
- Field types match schema definitions (e.g., `MaxUsers` is `int`)
- License conforms to expected structure
- Extra fields are allowed (not enforced by schema)

**Code Example:**
```csharp
var schema = LicenseSchema.FromYaml(File.ReadAllText("schema.yaml"));
var validator = new LicenseValidator(schema);

var license = new License();
license["LicenseId"] = "LIC-2026-001";
license["ExpiryUtc"] = DateTime.UtcNow.AddYears(1);
license["Signature"] = "ABC123";
license["CustomerName"] = "Acme Corp";  // Required by schema

bool isValid = validator.Validate(license, out var errors);
// ✓ Valid if all required fields present with correct types
```

## Why Two Tiers?

| Aspect | Field-Level | Schema-Level |
|--------|-------------|--------------|
| **Focus** | Data quality | Structural compliance |
| **Granularity** | Individual fields | Complete license |
| **Timing** | On field assignment | On explicit validation |
| **Reusability** | Validators shared across all licenses | Schema-specific |
| **Flexibility** | Can set fields without schema | Requires schema definition |

## Architecture Components

### 1. FieldValidators (Field-Level)

**Location:** [`FieldValidators.cs`](FieldValidators.cs)

**Key Features:**
- **Auto-discovery**: Validators marked with `[FieldValidator("FieldName")]` are automatically registered
- **Global registry**: Validators are shared across all `License` instances
- **Extensible**: Easy to add custom validators

**Default Validators:**
- `LicenseId`: Non-empty string validator
- `ExpiryUtc`: DateTime validator (supports multiple date formats)
- `Signature`: Optional string validator
- `MaxUsers`: Positive integer validator (example)
- `CustomerName`: Non-empty string validator (example)

**Adding Custom Validators:**

```csharp
// Method 1: Using attribute (auto-discovered)
[FieldValidator("CompanySize")]
public static ValidationResult ValidateCompanySize(object? value)
{
    if (value is null) return ValidationResult.Success(null);
    
    if (TypeChecking.IsNumeric(value, out var num))
    {
        if (num < 1 || num > 10000)
            return ValidationResult.Fail("CompanySize must be between 1 and 10,000");
        return ValidationResult.Success((int)num);
    }
    
    return ValidationResult.Fail("CompanySize must be numeric");
}

// Method 2: Manual registration
FieldValidators.Register("CustomField", value =>
{
    // Your validation logic
    return ValidationResult.Success(value);
});
```

### 2. License (Field-Level Application)

**Location:** [`../License.cs`](../License.cs)

**Key Features:**
- Uses `FieldValidators` registry for validation
- Validates on field assignment (setter)
- Normalizes field values automatically
- Throws `LicenseValidationException` on validation failure

**Important Methods:**
- `this[string fieldName]` - Property accessor with validation
- `SetField(name, value, out error)` - Explicit field setter with error output
- `EnsureMandatoryPresent()` - Validates mandatory fields only

### 3. LicenseValidator (Schema-Level)

**Location:** [`LicenseValidator.cs`](LicenseValidator.cs)

**Key Features:**
- Validates against `LicenseSchema` definitions
- Checks required fields and types
- Independent of field-level validation
- Collects all errors before reporting

**Important Methods:**
- `Validate(license, out errors)` - Returns bool with error list
- `ValidateOrThrow(license)` - Throws exception on failure
- `GetSchemaSummary()` - Returns human-readable schema info

### 4. LicenseSchema (Schema Definition)

**Location:** [`Schema.cs`](Schema.cs)

**Key Features:**
- Defines expected license structure
- Specifies field types, requirements, and defaults
- Supports JSON and YAML formats

**Schema Example:**
```json
{
  "Name": "BasicLicense",
  "Fields": [
    { "Name": "LicenseId", "Type": "string", "Required": true },
    { "Name": "ExpiryUtc", "Type": "datetime", "Required": true },
    { "Name": "Signature", "Type": "string", "Required": true },
    { "Name": "CustomerName", "Type": "string", "Required": true },
    { "Name": "MaxUsers", "Type": "int", "Required": false },
    { "Name": "Features", "Type": "list<string>", "Required": false }
  ]
}
```

## Validation Workflow

### Complete Example

```csharp
using SimpleLicense.Core;
using SimpleLicense.Core.LicenseValidation;

// 1. Load schema
var schemaYaml = File.ReadAllText("license-schema.yaml");
var schema = LicenseSchema.FromYaml(schemaYaml);

// 2. Create license document
var license = new License();

// 3. Set fields (field-level validation happens here)
try
{
    license["LicenseId"] = "LIC-2026-001";
    license["ExpiryUtc"] = "2027-12-31";  // Auto-parsed to DateTime
    license["Signature"] = "ABC123XYZ";
    license["CustomerName"] = "Acme Corporation";
    license["MaxUsers"] = 100;
}
catch (LicenseValidationException ex)
{
    Console.WriteLine($"Field validation failed: {ex.Message}");
    return;
}

// 4. Validate against schema (schema-level validation)
var validator = new LicenseValidator(schema);
if (validator.Validate(license, out var errors))
{
    Console.WriteLine("✓ License is valid!");
}
else
{
    Console.WriteLine("✗ Schema validation failed:");
    foreach (var error in errors)
    {
        Console.WriteLine($"  - {error}");
    }
}
```

## Validation Error Handling

### Field-Level Errors

**When:** Setting a field value  
**How:** Throws `LicenseValidationException` immediately

```csharp
try
{
    license["MaxUsers"] = "not a number";
}
catch (LicenseValidationException ex)
{
    // ex.Issues contains list of specific errors
    Console.WriteLine(ex.Message);
}
```

### Schema-Level Errors

**When:** Calling `validator.Validate()`  
**How:** Returns `false` with error list

```csharp
if (!validator.Validate(license, out var errors))
{
    foreach (var error in errors)
    {
        Console.WriteLine($"Error: {error}");
    }
}

// Or use ValidateOrThrow
try
{
    validator.ValidateOrThrow(license);
}
catch (LicenseValidationException ex)
{
    Console.WriteLine(ex.Message);
}
```

## Supported Field Types

### Field-Level (FieldValidators)
- `string` - Text values
- `int`, `long`, `float`, `double` - Numeric values
- `DateTime`, `DateTimeOffset` - Date/time values
- Custom types via validator functions

### Schema-Level (LicenseValidator)
- `string` - String type
- `int`, `integer` - Integer numbers
- `double`, `float`, `number` - Numeric values
- `bool`, `boolean` - Boolean values
- `datetime`, `date` - Date/time values
- `list<T>` - Lists with element type (e.g., `list<string>`)

## Best Practices

### 1. When to Use Field-Level Validation
- ✓ Enforce data format and quality
- ✓ Normalize input values
- ✓ Validate as data is entered
- ✓ Provide immediate feedback

### 2. When to Use Schema-Level Validation
- ✓ Validate complete license before use
- ✓ Ensure structural requirements are met
- ✓ Verify conformance to specifications
- ✓ Batch validation of all fields

### 3. Custom Validator Guidelines
- Keep validators pure (no side effects)
- Return normalized values in `ValidationResult.Success()`
- Provide clear, specific error messages
- Handle null values appropriately
- Use `TypeChecking.IsNumeric()` for number validation
- Use `TypeChecking.DescribeType()` for error messages

### 4. Error Handling Strategy
```csharp
// Strategy 1: Validate early (field-level)
try
{
    license["MaxUsers"] = userInput;
}
catch (LicenseValidationException ex)
{
    ShowUserError(ex.Message);
    return;
}

// Strategy 2: Batch validation (schema-level)
if (!validator.Validate(license, out var errors))
{
    ShowAllErrors(errors);
    return;
}

// Both are valid - choose based on UX requirements
```

## Testing

See the test suite in [`tests/SimpleLicense.Tests/`](../../../../tests/SimpleLicense.Tests/) for comprehensive test cases demonstrating:
- Valid license validation
- Missing required field detection
- Wrong type detection
- Extra fields handling

## Summary

The two-tier validation system provides:

✓ **Clear separation** between data quality and structural compliance  
✓ **Flexibility** to validate at different stages  
✓ **Reusability** of field validators across all licenses  
✓ **Extensibility** through custom validators and schemas  
✓ **User-friendly** error reporting at both levels  

Choose the validation tier based on your needs:
- **Field-level**: Immediate feedback, data quality
- **Schema-level**: Structural compliance, batch validation
