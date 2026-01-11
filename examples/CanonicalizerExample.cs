using SimpleLicense.Core.Canonicalizers;

namespace SimpleLicense.Examples
{
    /// <summary>
    /// Demonstrates file canonicalization for different file types.
    /// Shows both GenericTextFileCanonicalizer and InpFileCanonicalizer in action.
    /// </summary>
    public static class CanonicalizerExample
    {
        public static void Run()
        {
            ConsoleHelper.WriteInfo("This example demonstrates:");
            ConsoleHelper.WriteInfo("  • GenericTextFileCanonicalizer for common text files");
            ConsoleHelper.WriteInfo("  • InpFileCanonicalizer for EPANET .inp files");
            ConsoleHelper.WriteInfo("  • Before/after comparison of canonicalization");
            Console.WriteLine();

            // ═══════════════════════════════════════════════════════
            // Step 1: Generic Text File Canonicalization - YAML Config
            // ═══════════════════════════════════════════════════════
            ConsoleHelper.WriteHighlight("Step 1: Canonicalizing YAML configuration file");
            ConsoleHelper.WriteSeparator();

            var yamlInput = @"# Database Configuration
database:
    host:     localhost     # Primary host
    port:     5432
    # Connection settings
    pool_size:    10

# Server Configuration
server:
    host:   0.0.0.0
    port:   8080    
    debug:  true   # Enable debug mode
";

            var genericCanonicalizer = new GenericTextFileCanonicalizer(new[] { ".yaml", ".yml" });
            var yamlCanonical = genericCanonicalizer.Canonicalize(yamlInput);

            ConsoleHelper.WriteInfo("Original YAML (with comments and extra whitespace):");
            Console.WriteLine(yamlInput);
            Console.WriteLine();

            ConsoleHelper.WriteSuccess("✓ Canonicalized YAML:");
            Console.WriteLine(yamlCanonical);
            Console.WriteLine();

            // ═══════════════════════════════════════════════════════
            // Step 2: Generic Text File Canonicalization - Python Code
            // ═══════════════════════════════════════════════════════
            ConsoleHelper.WriteHighlight("Step 2: Canonicalizing Python code");
            ConsoleHelper.WriteSeparator();

            var pythonInput = @"# Calculate factorial
def factorial(n):
    # Base case
    if n <= 1:
        return   1    
    # Recursive case
    return   n   *   factorial(n - 1)

# Test the function
result = factorial(5)
print(result)   # Should be 120
";

            var pythonCanonical = genericCanonicalizer.Canonicalize(pythonInput);

            ConsoleHelper.WriteInfo("Original Python (with comments and extra whitespace):");
            Console.WriteLine(pythonInput);
            Console.WriteLine();

            ConsoleHelper.WriteSuccess("✓ Canonicalized Python (indentation preserved):");
            Console.WriteLine(pythonCanonical);
            Console.WriteLine();

            // ═══════════════════════════════════════════════════════
            // Step 3: Generic Text File Canonicalization - CSV Data
            // ═══════════════════════════════════════════════════════
            ConsoleHelper.WriteHighlight("Step 3: Canonicalizing CSV data");
            ConsoleHelper.WriteSeparator();

            var csvInput = @"# Customer data export
name,    age,    city,    country
John Doe,    25,    New York,    USA
Jane Smith,    30,    London,    UK

# End of data
";

            var csvCanonical = genericCanonicalizer.Canonicalize(csvInput);

            ConsoleHelper.WriteInfo("Original CSV (with comments and inconsistent spacing):");
            Console.WriteLine(csvInput);
            Console.WriteLine();

            ConsoleHelper.WriteSuccess("✓ Canonicalized CSV:");
            Console.WriteLine(csvCanonical);
            Console.WriteLine();

            // ═══════════════════════════════════════════════════════
            // Step 4: INP File Canonicalization - EPANET Network
            // ═══════════════════════════════════════════════════════
            ConsoleHelper.WriteHighlight("Step 4: Canonicalizing EPANET .inp file");
            ConsoleHelper.WriteSeparator();

            var inpInput = @"[TITLE]
Sample Water Distribution Network
Created for demonstration purposes
Version 1.0

; Network junction data
[JUNCTIONS]
;ID    Elev    Demand    Pattern
J1     100     50        1          ; Main junction
J2     120     75        1          ; Secondary junction
J3     110     0                    ; Zero demand node

[PIPES]
;ID    Node1    Node2    Length    Diameter    Roughness
P1     J1       J2       1000      12          100
P2     J2       J3       1500      8           100    ; Smaller pipe

[END]
";

            var inpCanonicalizer = new InpFileCanonicalizer();
            var inpCanonical = inpCanonicalizer.Canonicalize(inpInput);

            ConsoleHelper.WriteInfo("Original EPANET .inp file:");
            Console.WriteLine(inpInput);
            Console.WriteLine();

            ConsoleHelper.WriteSuccess("✓ Canonicalized .inp file:");
            ConsoleHelper.WriteInfo("  • [TITLE] section removed");
            ConsoleHelper.WriteInfo("  • Comments stripped");
            ConsoleHelper.WriteInfo("  • Section headers uppercased");
            ConsoleHelper.WriteInfo("  • Whitespace normalized");
            Console.WriteLine();
            Console.WriteLine(inpCanonical);
            Console.WriteLine();

            // ═══════════════════════════════════════════════════════
            // Step 5: Demonstrating Idempotency
            // ═══════════════════════════════════════════════════════
            ConsoleHelper.WriteHighlight("Step 5: Demonstrating idempotency");
            ConsoleHelper.WriteSeparator();

            var firstPass = genericCanonicalizer.Canonicalize(yamlInput);
            var secondPass = genericCanonicalizer.Canonicalize(firstPass);
            var thirdPass = genericCanonicalizer.Canonicalize(secondPass);

            if (firstPass == secondPass && secondPass == thirdPass)
            {
                ConsoleHelper.WriteSuccess("✓ Canonicalization is idempotent!");
                ConsoleHelper.WriteInfo("  Running canonicalize() multiple times produces identical results");
            }
            else
            {
                ConsoleHelper.WriteError("✗ Idempotency check failed");
            }
            Console.WriteLine();

            // ═══════════════════════════════════════════════════════
            // Step 6: File Writing and Reading Example
            // ═══════════════════════════════════════════════════════
            ConsoleHelper.WriteHighlight("Step 6: Saving canonicalized files");
            ConsoleHelper.WriteSeparator();

            // Save canonicalized examples
            var outputDir = "./outputs";
            Directory.CreateDirectory(outputDir);

            File.WriteAllText(Path.Combine(outputDir, "canonical_config.yaml"), yamlCanonical);
            File.WriteAllText(Path.Combine(outputDir, "canonical_data.csv"), csvCanonical);
            File.WriteAllText(Path.Combine(outputDir, "canonical_network.inp"), inpCanonical);

            ConsoleHelper.WriteSuccess("✓ Saved canonicalized files:");
            ConsoleHelper.WriteInfo($"  • {Path.Combine(outputDir, "canonical_config.yaml")}");
            ConsoleHelper.WriteInfo($"  • {Path.Combine(outputDir, "canonical_data.csv")}");
            ConsoleHelper.WriteInfo($"  • {Path.Combine(outputDir, "canonical_network.inp")}");
            Console.WriteLine();

            // ═══════════════════════════════════════════════════════
            // Step 7: Summary of Canonicalization Rules
            // ═══════════════════════════════════════════════════════
            ConsoleHelper.WriteHighlight("Step 7: Canonicalization rules summary");
            ConsoleHelper.WriteSeparator();

            ConsoleHelper.WriteInfo("GenericTextFileCanonicalizer:");
            Console.WriteLine("  • Normalizes line endings to \\n");
            Console.WriteLine("  • Removes trailing whitespace");
            Console.WriteLine("  • Preserves leading indentation (spaces and tabs)");
            Console.WriteLine("  • Collapses internal whitespace runs");
            Console.WriteLine("  • Strips full-line comments (#, ;, //)");
            Console.WriteLine("  • Removes empty lines");
            Console.WriteLine("  • Ensures single trailing newline");
            Console.WriteLine();

            ConsoleHelper.WriteInfo("InpFileCanonicalizer:");
            Console.WriteLine("  • Normalizes line endings to \\n");
            Console.WriteLine("  • Removes entire [TITLE] section");
            Console.WriteLine("  • Strips all comments (;)");
            Console.WriteLine("  • Uppercases section headers");
            Console.WriteLine("  • Trims and collapses all whitespace");
            Console.WriteLine("  • Removes empty lines");
            Console.WriteLine("  • Ensures single trailing newline");
            Console.WriteLine();
        }
    }
}
