using System.IO;

namespace SimpleLicense.Examples
{
    /// <summary>
    /// Helper class for colored console output.
    /// </summary>
    public static class ConsoleHelper
    {
        // ANSI color codes
        private const string Reset = "\u001b[0m";
        private const string Bold = "\u001b[1m";
        private const string Green = "\u001b[32m";
        private const string Red = "\u001b[31m";
        private const string Yellow = "\u001b[33m";
        private const string Blue = "\u001b[34m";
        private const string Cyan = "\u001b[36m";
        private const string Magenta = "\u001b[35m";
        private const string Gray = "\u001b[90m";

        public static void WriteHeader(string text)
        {
            Console.WriteLine($"{Bold}{Cyan}{text}{Reset}");
        }

        public static void WriteSubHeader(string text)
        {
            Console.WriteLine($"{Bold}{Blue}{text}{Reset}");
        }

        public static void WriteSuccess(string text)
        {
            Console.WriteLine($"{Green}{text}{Reset}");
        }

        public static void WriteError(string text)
        {
            Console.WriteLine($"{Red}{text}{Reset}");
        }

        public static void WriteWarning(string text)
        {
            Console.WriteLine($"{Yellow}{text}{Reset}");
        }

        public static void WriteInfo(string text)
        {
            Console.WriteLine($"{Gray}{text}{Reset}");
        }

        public static void WriteHighlight(string text)
        {
            Console.WriteLine($"{Bold}{Magenta}{text}{Reset}");
        }

        public static void WriteSeparator()
        {
            Console.WriteLine($"{Gray}───────────────────────────────────────────────────────{Reset}");
        }

        public static void WriteDoubleSeparator()
        {
            Console.WriteLine($"{Bold}{Cyan}═══════════════════════════════════════════════════════{Reset}");
        }
    }

    /// <summary>
    /// Entry point for SimpleLicense examples.
    /// Run individual examples to learn how to use the library.
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine();
            ConsoleHelper.WriteHeader("         SimpleLicense - Examples");
            Console.WriteLine();

            if (args.Length > 0 && args[0].ToLower() == "list")
            {
                ListExamples();
                return;
            }

            var outputs_folder = "./outputs";
            Directory.CreateDirectory(outputs_folder); // checks if the directory exists before creating one

            // Run all examples by default
            RunExample("License Creation and Validation", 1, LicenseCreationExample.Run);
            RunExample("File Canonicalization", 2, CanonicalizerExample.Run);

            Console.WriteLine();
            ConsoleHelper.WriteDoubleSeparator();
            ConsoleHelper.WriteSuccess("All examples completed!");
            ConsoleHelper.WriteDoubleSeparator();
        }

        private static void ListExamples()
        {
            ConsoleHelper.WriteSubHeader("Available Examples:");
            Console.WriteLine("  1. License Creation and Validation");
            Console.WriteLine("  2. File Canonicalization");
            Console.WriteLine();
            ConsoleHelper.WriteInfo("Run 'dotnet run' to execute all examples");
        }

        private static void RunExample(string name, int example_number, Action exampleAction)
        {
            ConsoleHelper.WriteSeparator();
            ConsoleHelper.WriteSubHeader($"Example {example_number}: {name}");
            ConsoleHelper.WriteSeparator();
            Console.WriteLine();

            try
            {
                exampleAction();
                Console.WriteLine();
                ConsoleHelper.WriteSuccess("✓ Example completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                ConsoleHelper.WriteError($"✗ Example failed: {ex.Message}");
                ConsoleHelper.WriteInfo($"  {ex.GetType().Name}");
            }

            Console.WriteLine();
        }
    }
}
