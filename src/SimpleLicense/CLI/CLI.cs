//
using Newtonsoft.Json.Serialization;
using Spectre.Console;
using Spectre.Console.Cli;
using System.IO;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.RegularExpressions;
using SimpleLicense.Core;
using SimpleLicense.Core.Cryptography;
using SimpleLicense.Core.LicenseValidation;
using SimpleLicense.Core.Utils;
using LicenseCore = SimpleLicense.Core.License;

namespace SimpleLicense.CLI
{

    public static class JsonConsole
    {
        /// <summary>
        /// Colourizes a pretty JSON string using Spectre.Console markup tags.
        /// </summary>
        public static string ColorizeJson(string prettyJson)
        {
            // Token regex: keys (string followed by :), other strings, numbers, booleans/null
            var tokenPattern = new Regex(
                @"(?<key>""(\\.|[^""\\])*""(?=\s*:))"   // JSON object key
            + @"|(?<str>""(\\.|[^""\\])*"")"         // JSON string value
            + @"|(?<num>-?\d+(\.\d+)?([eE][+-]?\d+)?)" // number
            + @"|(?<bool>\btrue\b|\bfalse\b|\bnull\b)", // boolean/null
                RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

            var sb = new System.Text.StringBuilder();
            int lastIndex = 0;

            foreach (Match m in tokenPattern.Matches(prettyJson))
            {
                // Append text between tokens (escaped for markup)
                if (m.Index > lastIndex)
                {
                    var between = prettyJson.Substring(lastIndex, m.Index - lastIndex);
                    sb.Append(Markup.Escape(between));
                }
                string token = m.Value;
                string escaped = Markup.Escape(token);
                if (m.Groups["key"].Success)
                {
                    // keys in green
                    sb.Append($"[green]{escaped}[/]");
                }
                else if (m.Groups["str"].Success)
                {
                    // string values in yellow
                    sb.Append($"[yellow]{escaped}[/]");
                }
                else if (m.Groups["num"].Success)
                {
                    sb.Append($"[cyan]{escaped}[/]");
                }
                else if (m.Groups["bool"].Success)
                {
                    sb.Append($"[magenta]{escaped}[/]");
                }
                else
                {
                    // fallback (shouldn't happen)
                    sb.Append(escaped);
                }
                lastIndex = m.Index + m.Length;
            }

            // Append remaining tail
            if (lastIndex < prettyJson.Length)
            {
                var tail = prettyJson.Substring(lastIndex);
                sb.Append(Markup.Escape(tail));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Pretty-prints an object as JSON and writes coloured output to the console.
        /// </summary>
        public static void WriteColoredJson(object obj, string title)
        {
            var pretty = JsonSerializer.Serialize(obj, new JsonSerializerOptions
            {
                WriteIndented = true,
                // Add converters if needed...
            });
            var colored = ColorizeJson(pretty);
            // Put it in a panel (optional)
            var panel = new Panel(new Markup(colored))
                .Header($"[bold]{title}[/]")
                .Border(BoxBorder.Rounded)
                .Expand();

            AnsiConsole.Write(panel);
        }
    }

    public static class StringExtensions
    {
        public static string Dedent(this string text)
        {
            var lines = text.Replace("\r", "").Split('\n');
            // Determine minimum indentation (exclude blank lines)
            var indent = lines
                .Where(l => l.Trim().Length > 0)
                .Select(l => l.TakeWhile(char.IsWhiteSpace).Count())
                .DefaultIfEmpty(0)
                .Min();
            // Remove that amount of indentation from each line
            var dedented = string.Join("\n",
                lines.Select(l =>
                    l.Length >= indent ? l.Substring(indent) : l
                )
            );
            return dedented.Trim('\n');
        }

        public static string Wrap(this string str, string color)
        {
            return $"[{color}]{str}[/]";
        }
    }

    internal static class Constants
    {
        public const string AppName = "SLiC";
    }

    public static class Program
    {
        public static int Main(string[] args)
        {
            var app = new CommandApp();

            app.Configure(config =>
            {
                config.SetApplicationName(Constants.AppName);
                config.AddCommand<InfoCommand>("info").WithDescription("Show info and help");
                config.AddCommand<KeyGeneratorCommand>("generate-keys").WithDescription("Generate an RSA key pair for license signing");
                config.AddCommand<HashFileCommand>("calculate-hash").WithDescription("Calculate hash of one or more files");
                config.AddCommand<CreateLicenseCommand>("create-license").WithDescription("Create and sign a license from schema");
                config.AddCommand<ValidateSchemaCommand>("validate-schema").WithDescription("Validate a license schema file");
                config.AddCommand<ValidateLicenseCommand>("validate-license").WithDescription("Validate and verify a signed license file");
            });
            // If user ran `myapp` with no args, treat it as `myapp info`
            if (args == null || args.Length == 0)
            {
                return app.Run(["info"]);
            }
            return app.Run(args);
        }
    }

    public class CommonAppSettings : CommandSettings
    {
        [CommandOption("-v|--verbose")]
        [Description("Enable verbose output.")]
        [DefaultValue(false)]
        public bool Verbose { get; init; }
    }

    public sealed class InfoSettings : CommandSettings { }

    public sealed class InfoCommand : AsyncCommand<InfoSettings>
    {
        public override Task<int> ExecuteAsync(
            [NotNull] CommandContext context,
            [NotNull] InfoSettings settings,
            CancellationToken cancellationToken)
        {
            AnsiConsole.Write(
                new FigletText(Constants.AppName).Centered().Color(Color.Red));
            AnsiConsole.Write(
                new Align(
                    new Markup(
                        "[cyan][bold]Simple License Creator [red](SLiC)[/][/] - " +
                        "A small utility for generating and validating JSON file licenses " + 
                        "for small research-grade projects.[/]"),
                    HorizontalAlignment.Center
                )
            );
            AnsiConsole.WriteLine("\n");
            var table = new Table()
                .AddColumn("Command")
                .AddColumn("Description");
            var create_license_text = $@"
                Create and sign a license from schema.
                [blue]Example:[/] [grey]
                {Constants.AppName.ToLower()} [yellow]create-license[/] 
                    -s schema.yaml 
                    --field LicenseId=ABC-123 
                    --field MaxUsers=50 
                    -k ./keys/private_key.pem 
                    -o license.json
                [/]".Dedent();
            var validate_schema_text = $@"
                Validate a license schema file.
                [blue]Example:[/] [grey]
                {Constants.AppName.ToLower()} [yellow]validate-schema[/]
                    -s schema.yaml
                [/]".Dedent();
            var calculate_hash_text = $@"
                Calculate a hash for one or more input files.
                [blue]Example:[/]: [grey]
                {Constants.AppName.ToLower()} [yellow]calculate-hash[/]
                    -i file1.inp -i file2.inp
                    -e utf-8
                [/]".Dedent();
            table.AddRow(
                "[yellow]generate-keys[/]",
                $"Generate an RSA key pair for license signing. [blue]Example:[/]: [grey]{Constants.AppName.ToLower()} " +
                "[yellow]generate-keys[/] -k 3072 -d ./keys[/]\n");
            table.AddRow(
                "[yellow]calculate-hash[/]",
                calculate_hash_text);
            table.AddRow(
                "[yellow]create-license[/]",
                create_license_text);
            table.AddRow(
                "[yellow]validate-schema[/]",
                validate_schema_text);
            table.AddRow(
                "[yellow]validate-license[/]",
                $"Validate signed license file. [blue]Example:[/]: [grey]{Constants.AppName.ToLower()} " +
                "[yellow]validate-license[/] -l license.json -k ./keys/public_key.pem[/]");

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"Usage: [bold]{Constants.AppName.ToLower()} [[command]] [[options]][/]");
            AnsiConsole.MarkupLine($"Run [green]{Constants.AppName.ToLower()} [[command]] --help[/] for command-specific options.");
            return Task.FromResult(0);
        }
    }

    public class KeyGeneratorSettings : CommonAppSettings
    {
        [CommandOption("-k|--keysize <BITS>")]
        [Description("RSA key size in bits (default: 2048). Minimum is 2048.")]
        [DefaultValue(2048)]
        public int KeySize { get; init; } = 2048;

        [CommandOption("-d|--keydir <PATH>")]
        [Description("Directory to save generated keys into (default: current directory).")]
        [DefaultValue(".")]
        public string KeyDir { get; init; } = ".";
    }

    public sealed class KeyGeneratorCommand : AsyncCommand<KeyGeneratorSettings>
    {
        public override Task<int> ExecuteAsync(
            [NotNull] CommandContext context,
            [NotNull] KeyGeneratorSettings settings,
            CancellationToken cancellationToken)
        {
            try
            {
                var keyGen = new KeyGenerator(keySize: settings.KeySize);
                keyGen.KeyDir = new DirectoryInfo(path: settings.KeyDir);
                
                if (settings.Verbose == true)
                {
                    keyGen.OnInfo += msg => AnsiConsole.MarkupLine($"[yellow]{msg}[/]");
                }
                AnsiConsole.MarkupLine($"[blue]Generating RSA key pair with size [bold]{settings.KeySize}[/] bits...[/]");
                var result = keyGen.GenerateKeys();

                if (result.Success)
                {
                    AnsiConsole.MarkupLine($"[bold green]Key pair generated successfully and saved in directory: [red]{keyGen.KeyDir.FullName}[/].[/]");
                    return Task.FromResult(0);
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]Key generation failed.[/]");
                    return Task.FromResult(1);
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything | ExceptionFormats.ShowLinks);
                return Task.FromResult(1);
            }
        }
    }

    public class HashInpFileSettings : CommonAppSettings
    {
        [CommandOption("-i|--input <PATH>")]
        [Description("Input file(s). Repeat the option to provide multiple files.")]
        public string[]? Inputs { get; init; }

        [CommandOption("-e|--encoding <NAME>")]
        [Description("Text encoding of the files (default: utf-8). Supported encodings: utf-8, utf-16, utf-16be, ascii.")]
        [DefaultValue("utf-8")]
        public string EncodingName { get; init; } = "utf-8";
    }

    public sealed class HashFileCommand : AsyncCommand<HashInpFileSettings>
    {
        public override async Task<int> ExecuteAsync(
            [NotNull] CommandContext context,
            [NotNull] HashInpFileSettings settings,
            CancellationToken cancellationToken)
        {
            var inputs = settings.Inputs ?? Array.Empty<string>();
            var encodingName = settings.EncodingName.ToLowerInvariant();

            AnsiConsole.WriteLine();
            if (inputs.Length == 0)
            {
                AnsiConsole.MarkupLine("[red]No input files supplied. Provide input files to hash using [grey]-i [[filename1]] -i [[filename2]] ... [/]syntax[/].");
                return 1;
            }
            AnsiConsole.MarkupLine($"Processing files with [blue][bold]{settings.EncodingName}[/][/] encoding:");

            if (settings.Verbose) {
                AnsiConsole.MarkupLine($"[bold]Files to process:[/]");
                var t = new Table().AddColumn("#").AddColumn("Path");
                for (int i = 0; i < inputs.Length; i++)
                {
                    t.AddRow((i + 1).ToString(), inputs[i]);
                }
                AnsiConsole.Write(t);
            }

            var fileHashes = new Dictionary<string, string>();
            await AnsiConsole.Progress()
                .AutoClear(false)
                .HideCompleted(false)
                .Columns(new ProgressColumn[] {
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new RemainingTimeColumn()
                })
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("Processing files", maxValue: inputs.Length);
                    for (int i = 0; i < inputs.Length; i++)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            AnsiConsole.MarkupLine("[red]Operation cancelled.[/]");
                            return;
                        }
                        var fileHash = await Task.Run(() => TextFileHasher.HashFile(
                            inputs[i], null), cancellationToken
                        );
                        fileHashes[inputs[i]] = fileHash;
                        task.Increment(1);
                    }
                });

            AnsiConsole.MarkupLine("[bold green]All files processed successfully.[/]");
            // Display results in a table
            var resultsTable = new Table()
                .AddColumn("File")
                .AddColumn("Hash")
                .Border(TableBorder.Rounded);

            foreach (var kvp in fileHashes)
            {
                resultsTable.AddRow(
                    $"[blue]{kvp.Key}[/]",
                    $"[yellow]{kvp.Value}[/]"
                );
            }
            AnsiConsole.Write(resultsTable);
            return 0;
        }
    }

    public class CreateLicenseSettings : CommonAppSettings
    {
        [CommandOption("-s|--schema <PATH>")]
        [Description("Path to the license schema file (JSON or YAML).")]
        public required string SchemaPath { get; init; }

        [CommandOption("--field <KEY=VALUE>")]
        [Description("Individual field as key=value pair. Can be repeated for multiple fields (e.g., --field LicenseId=ABC-123 --field MaxUsers=50).")]
        public string[]? Fields { get; init; }

        [CommandOption("--field-values <JSON>")]
        [Description("Field values as JSON string. Use --field instead for easier syntax.")]
        public string? ValuesJson { get; init; }

        [CommandOption("-f|--values-file <PATH>")]
        [Description("Path to a JSON file containing field values.")]
        public string? ValuesFilePath { get; init; }

        [CommandOption("-k|--keyfile <PATH>")]
        [Description("Path to the RSA private key file used for signing the license.")]
        public required string KeyFilePath { get; init; }

        [CommandOption("-o|--output <PATH>")]
        [Description("Output license file path.")]
        public string OutputPath { get; init; } = "license.json";

        [CommandOption("-w|--workdir <PATH>")]
        [Description("Working directory for file processors (default: current directory).")]
        [DefaultValue(".")]
        public string WorkingDirectory { get; init; } = ".";

        [CommandOption("--no-validate")]
        [Description("Skip schema validation after creating the license.")]
        [DefaultValue(false)]
        public bool NoValidate { get; init; }
        [Description("Text encoding of the input files (default: utf-8). Supported encodings: utf-8, utf-16, utf-16be, ascii.")]
        [DefaultValue("utf-8")]
        public string EncodingName { get; init; } = "utf-8";
    }

    public sealed class CreateLicenseCommand : AsyncCommand<CreateLicenseSettings>
    {
        // Helper to convert JsonElement to proper .NET types
        private static Dictionary<string, object?> ConvertJsonElements(Dictionary<string, object?> input)
        {
            var result = new Dictionary<string, object?>();
            foreach (var kvp in input)
            {
                if (kvp.Value is JsonElement element)
                {
                    result[kvp.Key] = element.ValueKind switch
                    {
                        JsonValueKind.String => element.GetString(),
                        JsonValueKind.Number => element.TryGetInt32(out var i) ? i : element.GetDouble(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Null => null,
                        _ => kvp.Value
                    };
                }
                else
                {
                    result[kvp.Key] = kvp.Value;
                }
            }
            return result;
        }

        // Helper to parse field key=value pairs
        private static Dictionary<string, object?> ParseFieldPairs(string[] fieldPairs)
        {
            var result = new Dictionary<string, object?>();
            foreach (var pair in fieldPairs)
            {
                var equalsIndex = pair.IndexOf('=');
                if (equalsIndex < 0)
                {
                    throw new InvalidOperationException($"Invalid field format: '{pair}'. Expected format: KEY=VALUE");
                }

                var key = pair.Substring(0, equalsIndex).Trim();
                var value = pair.Substring(equalsIndex + 1);

                if (string.IsNullOrWhiteSpace(key))
                {
                    throw new InvalidOperationException($"Invalid field format: '{pair}'. Key cannot be empty.");
                }

                // Try to parse as number or boolean, otherwise keep as string
                object? parsedValue;
                if (int.TryParse(value, out var intVal))
                {
                    parsedValue = intVal;
                }
                else if (double.TryParse(value, out var doubleVal))
                {
                    parsedValue = doubleVal;
                }
                else if (bool.TryParse(value, out var boolVal))
                {
                    parsedValue = boolVal;
                }
                else if (value.Equals("null", StringComparison.OrdinalIgnoreCase))
                {
                    parsedValue = null;
                }
                else
                {
                    parsedValue = value;
                }

                result[key] = parsedValue;
            }
            return result;
        }

        public override async Task<int> ExecuteAsync(
            [NotNull] CommandContext context,
            [NotNull] CreateLicenseSettings settings,
            CancellationToken cancellationToken)
        {
            
            AnsiConsole.WriteLine("");
            AnsiConsole.MarkupLine("[cyan]======= Creating License =======[/]");
            AnsiConsole.MarkupLine($"[grey]Output              :[/] {settings.OutputPath}");
            try
            {
                AnsiConsole.WriteLine("");
                AnsiConsole.MarkupLine("[cyan]======= Creating License =======[/]");
                AnsiConsole.MarkupLine($"[grey]Schema file    :[/] {settings.SchemaPath}");
                AnsiConsole.MarkupLine($"[grey]Key file       :[/] {settings.KeyFilePath}");
                AnsiConsole.MarkupLine($"[grey]Output file    :[/] {settings.OutputPath}");
                AnsiConsole.MarkupLine($"[grey]Working dir    :[/] {settings.WorkingDirectory}");
                AnsiConsole.MarkupLine("");

                // Load schema
                if (!File.Exists(settings.SchemaPath))
                {
                    AnsiConsole.MarkupLine($"[red]Schema file not found: {settings.SchemaPath}[/]");
                    return 1;
                }

                AnsiConsole.MarkupLine("[cyan]Loading schema...[/]");
                var schemaJson = await File.ReadAllTextAsync(settings.SchemaPath, cancellationToken);
                var schema = settings.SchemaPath.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
                             settings.SchemaPath.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
                    ? LicenseSchema.FromYaml(schemaJson)
                    : LicenseSchema.FromJson(schemaJson);

                AnsiConsole.MarkupLine($"[green]✔[/] Schema loaded: [bold]{schema.Name}[/] ({schema.Fields.Count} fields)");

                // Load field values
                Dictionary<string, object?> fieldValues;
                if (settings.Fields != null && settings.Fields.Length > 0)
                {
                    // Parse individual field pairs (preferred method)
                    AnsiConsole.MarkupLine("[cyan]Parsing field values from command line...[/]");
                    fieldValues = ParseFieldPairs(settings.Fields);
                }
                else if (!string.IsNullOrWhiteSpace(settings.ValuesFilePath))
                {
                    if (!File.Exists(settings.ValuesFilePath))
                    {
                        AnsiConsole.MarkupLine($"[red]Values file not found: {settings.ValuesFilePath}[/]");
                        return 1;
                    }
                    AnsiConsole.MarkupLine($"[cyan]Loading field values from file...[/]");
                    var valuesJson = await File.ReadAllTextAsync(settings.ValuesFilePath, cancellationToken);
                    var rawValues = JsonSerializer.Deserialize<Dictionary<string, object?>>(valuesJson)
                        ?? new Dictionary<string, object?>();
                    fieldValues = ConvertJsonElements(rawValues);
                }
                else if (!string.IsNullOrWhiteSpace(settings.ValuesJson))
                {
                    AnsiConsole.MarkupLine("[cyan]Parsing field values from JSON string...[/]");
                    var rawValues = JsonSerializer.Deserialize<Dictionary<string, object?>>(settings.ValuesJson)
                        ?? new Dictionary<string, object?>();
                    fieldValues = ConvertJsonElements(rawValues);
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]No field values provided. License will be created with empty/processed fields only.[/]");
                    fieldValues = new Dictionary<string, object?>();
                }

                if (settings.Verbose && fieldValues.Any())
                {
                    AnsiConsole.MarkupLine("[cyan]Input field values:[/]");
                    foreach (var kvp in fieldValues)
                    {
                        AnsiConsole.MarkupLine($"  • {kvp.Key} = {kvp.Value}");
                    }
                }

                // Create license
                AnsiConsole.MarkupLine("\n[cyan]Creating license from schema...[/]");
                var licenseCreator = new LicenseCreator();
                if (settings.Verbose)
                {
                    licenseCreator.OnInfo += msg => AnsiConsole.MarkupLine($"[yellow]{msg}[/]");
                }

                cancellationToken.ThrowIfCancellationRequested();
                var licenseUnsigned = licenseCreator.CreateLicense(
                    schema: schema,
                    fieldValues: fieldValues,
                    workingDirectory: settings.WorkingDirectory,
                    processorParameters: null,
                    validateSchema: !settings.NoValidate
                );

                AnsiConsole.MarkupLine("[green]✔[/] License created successfully");
                
                if (settings.Verbose)
                {
                    JsonConsole.WriteColoredJson(licenseUnsigned, "Unsigned License");
                }

                // Load private key
                if (!File.Exists(settings.KeyFilePath))
                {
                    AnsiConsole.MarkupLine($"[red]Private key file not found: {settings.KeyFilePath}[/]");
                    return 1;
                }

                AnsiConsole.MarkupLine("\n[cyan]Signing license...[/]");
                var privateKey = await File.ReadAllTextAsync(settings.KeyFilePath, cancellationToken);
                var licenseSigner = new LicenseSigner(privateKey);
                
                cancellationToken.ThrowIfCancellationRequested();
                var licenseSigned = licenseSigner.SignLicenseDocument(licenseUnsigned);
                
                AnsiConsole.MarkupLine("[green]✔[/] License signed successfully");
                
                if (settings.Verbose)
                {
                    var signature = licenseSigned["Signature"]?.ToString();
                    var sigPreview = signature?.Length > 40 
                        ? signature.Substring(0, 40) + "..." 
                        : signature;
                    AnsiConsole.MarkupLine($"  Signature: [grey]{sigPreview}[/]");
                }

                // Save license
                var fullOutput = Path.GetFullPath(settings.OutputPath);
                AnsiConsole.MarkupLine($"\n[cyan]Saving license to:[/] {fullOutput}");
                
                var prettySerializer = new LicenseSerializer { Options = JsonProfiles.Pretty };
                var licenseOutput = prettySerializer.SerializeLicenseDocument(licenseSigned);
                
                await File.WriteAllTextAsync(settings.OutputPath, licenseOutput, cancellationToken);
                
                AnsiConsole.MarkupLine("[bold green]✔ License created, signed, and saved successfully![/]");
                
                return 0;
            }
            catch (OperationCanceledException)
            {
                AnsiConsole.MarkupLine("[red]Operation cancelled.[/]");
                return 2;
            }
            catch (JsonException ex)
            {
                AnsiConsole.MarkupLine($"[red]JSON parsing error: {ex.Message}[/]");
                if (settings.Verbose) AnsiConsole.WriteException(ex);
                return 1;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                if (settings.Verbose) AnsiConsole.WriteException(ex);
                return 1;
            }
        }
    }

    public class ValidateSchemaSettings : CommonAppSettings
    {
        [CommandOption("-s|--schema <PATH>")]
        [Description("Path to the license schema file (JSON or YAML).")]
        public required string SchemaPath { get; init; }
    }

    public sealed class ValidateSchemaCommand : AsyncCommand<ValidateSchemaSettings>
    {
        public override async Task<int> ExecuteAsync(
            [NotNull] CommandContext context,
            [NotNull] ValidateSchemaSettings settings,
            CancellationToken cancellationToken)
        {
            try
            {
                AnsiConsole.MarkupLine("[cyan]======= Validating Schema =======[/]");
                AnsiConsole.MarkupLine($"[grey]Schema file:[/] {settings.SchemaPath}");
                AnsiConsole.WriteLine();

                if (!File.Exists(settings.SchemaPath))
                {
                    AnsiConsole.MarkupLine($"[red]Schema file not found: {settings.SchemaPath}[/]");
                    return 1;
                }

                var schemaContent = await File.ReadAllTextAsync(settings.SchemaPath, cancellationToken);
                
                LicenseSchema schema;
                if (settings.SchemaPath.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
                    settings.SchemaPath.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
                {
                    AnsiConsole.MarkupLine("[cyan]Parsing YAML schema...[/]");
                    schema = LicenseSchema.FromYaml(schemaContent);
                }
                else
                {
                    AnsiConsole.MarkupLine("[cyan]Parsing JSON schema...[/]");
                    schema = LicenseSchema.FromJson(schemaContent);
                }

                AnsiConsole.MarkupLine($"[green]✔[/] Schema is valid!");
                AnsiConsole.WriteLine();
                
                // Display schema details
                var table = new Table()
                    .Border(TableBorder.Rounded)
                    .AddColumn("Field Name")
                    .AddColumn("Type")
                    .AddColumn("Required")
                    .AddColumn("Signed")
                    .AddColumn("Processor");

                foreach (var field in schema.Fields)
                {
                    table.AddRow(
                        $"[cyan]{field.Name}[/]",
                        $"[yellow]{field.Type}[/]",
                        field.Required ? "[green]Yes[/]" : "[grey]No[/]",
                        field.Signed ? "[green]Yes[/]" : "[grey]No[/]",
                        field.Processor ?? "[grey]-[/]"
                    );
                }

                AnsiConsole.Write(
                    new Panel(table)
                        .Header($"[bold]Schema: {schema.Name}[/]")
                        .Expand()
                );

                return 0;
            }
            catch (JsonException ex)
            {
                AnsiConsole.MarkupLine($"[red]JSON parsing error: {ex.Message}[/]");
                if (settings.Verbose) AnsiConsole.WriteException(ex);
                return 1;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                if (settings.Verbose) AnsiConsole.WriteException(ex);
                return 1;
            }
        }
    }

    public class ValidateLicenseSettings : CommonAppSettings
    {
        [CommandOption("-l|--license <PATH>")]
        [Description("Path to the license file to validate.")]
        public required string LicenseFile { get; init; }

        [CommandOption("-k|--public-key-file <PATH>")]
        [Description("Path to the RSA public key file (for signature verification).")]
        public string? KeyFilePath { get; init; }

        [CommandOption("-s|--schema <PATH>")]
        [Description("Path to the schema file (for structure validation).")]
        public string? SchemaPath { get; init; }
    }

    public sealed class ValidateLicenseCommand : AsyncCommand<ValidateLicenseSettings>
    {
        public override async Task<int> ExecuteAsync(
            [NotNull] CommandContext context, 
            [NotNull] ValidateLicenseSettings settings, 
            CancellationToken cancellationToken)
        {
            try
            {
                AnsiConsole.MarkupLine("[cyan]======= Validating License =======[/]");
                AnsiConsole.MarkupLine($"[grey]License file:[/] {settings.LicenseFile}");
                
                if (!File.Exists(settings.LicenseFile))
                {
                    AnsiConsole.MarkupLine($"[red]License file not found: {settings.LicenseFile}[/]");
                    return 1;
                }

                var licenseJson = await File.ReadAllTextAsync(settings.LicenseFile, cancellationToken);
                bool allValid = true;

                // Signature verification if public key provided
                if (!string.IsNullOrWhiteSpace(settings.KeyFilePath))
                {
                    if (!File.Exists(settings.KeyFilePath))
                    {
                        AnsiConsole.MarkupLine($"[red]Public key file not found: {settings.KeyFilePath}[/]");
                        return 1;
                    }

                    AnsiConsole.MarkupLine($"[grey]Public key  :[/] {settings.KeyFilePath}");
                    AnsiConsole.WriteLine();
                    
                    AnsiConsole.MarkupLine("[cyan]Verifying signature...[/]");
                    var publicKey = await File.ReadAllTextAsync(settings.KeyFilePath, cancellationToken);
                    var verifier = new LicenseVerifier(publicKey);
                    
                    cancellationToken.ThrowIfCancellationRequested();
                    bool isValid = verifier.VerifyLicenseDocumentJson(licenseJson, out string? failureReason);
                    
                    if (isValid)
                    {
                        AnsiConsole.MarkupLine("[green]✔[/] Signature is valid");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]✘[/] Signature verification failed: {failureReason}");
                        allValid = false;
                    }
                }

                // Schema validation if schema provided
                if (!string.IsNullOrWhiteSpace(settings.SchemaPath))
                {
                    if (!File.Exists(settings.SchemaPath))
                    {
                        AnsiConsole.MarkupLine($"[red]Schema file not found: {settings.SchemaPath}[/]");
                        return 1;
                    }

                    AnsiConsole.MarkupLine($"[grey]Schema file :[/] {settings.SchemaPath}");
                    AnsiConsole.WriteLine();
                    
                    AnsiConsole.MarkupLine("[cyan]Validating against schema...[/]");
                    var schemaContent = await File.ReadAllTextAsync(settings.SchemaPath, cancellationToken);
                    var schema = settings.SchemaPath.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
                                 settings.SchemaPath.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
                        ? LicenseSchema.FromYaml(schemaContent)
                        : LicenseSchema.FromJson(schemaContent);

                    var license = LicenseCore.FromJson(licenseJson);
                    var validator = new LicenseValidator(schema);
                    
                    cancellationToken.ThrowIfCancellationRequested();
                    var isValid = validator.Validate(license, out var errors);
                    
                    if (isValid)
                    {
                        AnsiConsole.MarkupLine("[green]✔[/] License structure is valid according to schema");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]✘[/] Schema validation failed:");
                        foreach (var error in errors)
                        {
                            AnsiConsole.MarkupLine($"  • [red]{error}[/]");
                        }
                        allValid = false;
                    }
                }

                // If neither key nor schema provided, just check if license can be parsed
                if (string.IsNullOrWhiteSpace(settings.KeyFilePath) && string.IsNullOrWhiteSpace(settings.SchemaPath))
                {
                    AnsiConsole.MarkupLine("[yellow]No public key or schema provided. Checking JSON syntax only...[/]");
                    AnsiConsole.WriteLine();
                    
                    var license = LicenseCore.FromJson(licenseJson);
                    AnsiConsole.MarkupLine("[green]✔[/] License JSON is well-formed");
                    
                    if (settings.Verbose)
                    {
                        AnsiConsole.WriteLine();
                        AnsiConsole.MarkupLine("[cyan]License fields:[/]");
                        foreach (var kvp in license.Fields)
                        {
                            var value = kvp.Value?.ToString();
                            var display = value?.Length > 50 ? value.Substring(0, 50) + "..." : value;
                            AnsiConsole.MarkupLine($"  • {kvp.Key} = [grey]{display}[/]");
                        }
                    }
                }

                AnsiConsole.WriteLine();
                if (allValid)
                {
                    AnsiConsole.MarkupLine("[bold green]✔ License validation passed![/]");
                    return 0;
                }
                else
                {
                    AnsiConsole.MarkupLine("[bold red]✘ License validation failed![/]");
                    return 1;
                }
            }
            catch (OperationCanceledException)
            {
                AnsiConsole.MarkupLine("[red]Operation cancelled.[/]");
                return 2;
            }
            catch (JsonException ex)
            {
                AnsiConsole.MarkupLine($"[red]Invalid JSON: {ex.Message}[/]");
                if (settings.Verbose) AnsiConsole.WriteException(ex);
                return 1;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                if (settings.Verbose) AnsiConsole.WriteException(ex);
                return 1;
            }
        }
    }
}