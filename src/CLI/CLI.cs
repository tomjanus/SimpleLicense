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
using SimpleLicense.Utils;
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
                config.AddCommand<HashInpFileCommand>("calculate-hash").WithDescription("Calculate hash of one or more files");
                config.AddCommand<CreateLicenseCommand>("create-license").WithDescription("Create and sign a simple license file in JSON format");
                config.AddCommand<ValidateLincenseCommand>("validate-license").WithDescription("Validate the simple JSON license file");
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
                Create and sign a license file.
                [blue]Example:[/] [grey]
                {Constants.AppName.ToLower()} [yellow]create-license[/] 
                    -o license.lic 
                    -k ./keys/private_key.pem 
                    -n max-number-of-network-junctions
                    -e expiry-in-months 
                    -d file-folder 
                    -f additional-file1 -f additional-file2
                    -id license-id 
                    -ai additional-info
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
                "[yellow]validate-license[/]",
                $"Validate signed license file. [blue]Example:[/]: [grey]{Constants.AppName.ToLower()} " +
                "[yellow]validate-license[/] -i license.lic -k ./keys/public_key.pem[/]");

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

    public sealed class HashInpFileCommand : AsyncCommand<HashInpFileSettings>
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
        [CommandOption("-o|--output <PATH>")]
        [Description("Output license file path.")]
        public string OutputPath { get; init; } = "license.lic";

        [CommandOption("-k|--keyfile <PATH>")]
        [Description("Path to the RSA private key file used for signing the license.")]
        public required string KeyFilePath { get; init; }

        [CommandOption("-n|--max-junctions <NUMBER>")]
        [Description("Maximum number of network junctions allowed by the license.")]
        public int? MaxJunctions { get; init; } = null;

        [CommandOption("-e|--expiry-months <NUMBER>")]
        [Description("License expiry time in months from the date of issue.")]
        public int? ExpiryMonths { get; init; } = null;

        [CommandOption("-d|--dir <PATH>")]
        [Description("Directory containing the main file to hash.")]
        public string? InpDir { get; init; } = null;

        [CommandOption("-f|--additional-file <PATH>")]
        [Description("Additional file(s) to include in the license hash. Repeat the option to provide multiple files.")]
        public string[]? AdditionalInpFiles { get; init; } = null;

        [CommandOption("--id <ID>")]
        [Description("Unique identifier for the license.")]
        public string? LicenseId { get; init; } = null;

        [CommandOption("--additional-info <INFO>")]
        [Description("Additional information to include in the license.")]
        public string? AdditionalInfo { get; init; } = null;

        [CommandOption("--encoding <NAME>")]
        [Description("Text encoding of the input files (default: utf-8). Supported encodings: utf-8, utf-16, utf-16be, ascii.")]
        [DefaultValue("utf-8")]
        public string EncodingName { get; init; } = "utf-8";
    }

    public sealed class CreateLicenseCommand : AsyncCommand<CreateLicenseSettings>
    {
        public override async Task<int> ExecuteAsync(
            [NotNull] CommandContext context,
            [NotNull] CreateLicenseSettings settings,
            CancellationToken cancellationToken)
        {
            
            AnsiConsole.WriteLine("");
            AnsiConsole.MarkupLine("[cyan]======= Creating License =======[/]");
            AnsiConsole.MarkupLine($"[grey]Output              :[/] {settings.OutputPath}");
            AnsiConsole.MarkupLine($"[grey]Key file            :[/] {settings.KeyFilePath}");
            AnsiConsole.MarkupLine($"[grey]Max junctions      :[/] {settings.MaxJunctions?.ToString() ?? "none"}");
            AnsiConsole.MarkupLine($"[grey]Expiry months       :[/] {settings.ExpiryMonths?.ToString() ?? "none"}");
            AnsiConsole.MarkupLine($"[grey]Input file folder   :[/] {settings.InpDir ?? "none"}");
            AnsiConsole.MarkupLine($"[grey]Extra files         :[/] {(settings.AdditionalInpFiles is null ? "none" : string.Join(", ", settings.AdditionalInpFiles))}");
            AnsiConsole.MarkupLine("");

            IEnumerable<string>? inpFiles = Enumerable.Empty<string>();
            
            // Find input files to process
            if (settings.InpDir is not null)
            {
                var folderSource = new FolderFileSource(
                    folder: settings.InpDir,
                    pattern: "*.inp",
                    recursive: true
                );
                AnsiConsole.MarkupLine($"[cyan]Scanning directory:[/] {settings.InpDir}");
                try
                {
                    inpFiles = folderSource.EnumerateFiles();
                }
                catch (DirectoryNotFoundException ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error enumerating input files in directory '{settings.InpDir}': {ex.Message} - Directory Not Found[/]");
                }
                catch (ArgumentException ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error enumerating input files in directory '{settings.InpDir}': {ex.Message} - Bad file-search pattern[/]");
                }
            }
            if (settings.AdditionalInpFiles is not null && settings.AdditionalInpFiles.Length > 0)
            {
                AnsiConsole.MarkupLine($"[cyan]Adding additional input files[/]");
                var listSource = new ListFileSource(settings.AdditionalInpFiles);
                inpFiles = inpFiles.Concat(listSource.EnumerateFiles());
            }
            if (!inpFiles.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No input files were found or provided.[/]");
                inpFiles = null;
            } else
            {
                AnsiConsole.MarkupLine("[cyan]The following input files will be hashed and added to the license:[/]");
                
                foreach (var file in inpFiles)
                {
                    string full = Path.GetFullPath(file);
                    AnsiConsole.MarkupLine($"  • [grey]{full}[/]");
                }

                AnsiConsole.MarkupLine(""); // spacing
            }

            AnsiConsole.MarkupLine("\n[cyan]Generating license...[/]");
            var licenseCreator = new LicenseCreator(licenseID: settings.LicenseId)
            {
                NumberOfValidMonths = settings.ExpiryMonths,
                MaxJunctions = settings.MaxJunctions,
                LicenseInfo = settings.AdditionalInfo,
                InputFiles = inpFiles
            };
            if (settings.Verbose)
            {
                licenseCreator.OnInfo += msg => 
                {
                    if (settings.Verbose)
                    {
                        AnsiConsole.MarkupLine($"[yellow]{msg}[/]");
                    }
                };                
            }

            try
            {
                var encoding = EncodingMap.GetEncoding(settings.EncodingName);
                // Create license (synchronous, presumably cheap)
                // If CreateLicense can be CPU-heavy, wrap in Task.Run
                cancellationToken.ThrowIfCancellationRequested();
                LicenseCore licenseUnsigned = licenseCreator.CreateLicense(encoding.EncodingName);
                LicenseSerializer prettySerializer = new LicenseSerializer()
                {
                    Options = JsonProfiles.Pretty
                }; 
                //string licenseSerialized = serializer.Serialize(licenseUnsigned);
                JsonConsole.WriteColoredJson(licenseUnsigned, "Unsigned License");
                AnsiConsole.MarkupLine("\n[cyan]Signing license...[/]");
                FileIO fileIO = new FileIO(encoding);
                string privateKey = fileIO.ReadAllText(settings.KeyFilePath);
                LicenseSigner licenseSigner = new LicenseSigner(privateKey);
                cancellationToken.ThrowIfCancellationRequested();
                LicenseCore licenseSigned = licenseSigner.SignLicense(licenseUnsigned);
                JsonConsole.WriteColoredJson(licenseSigned, "Signed License");
                string fullOutput = Path.GetFullPath(settings.OutputPath);
                AnsiConsole.MarkupLine($"[cyan]Saving license to:[/] {fullOutput}");
                string licenseOutput = prettySerializer.Serialize(licenseSigned);
                // Write signed license to disk (offload)
                await Task.Run(() => fileIO.WriteAllText(settings.OutputPath, licenseOutput), cancellationToken);
                AnsiConsole.MarkupLine("[green]✔ License created and saved successfully![/]");
                return 0;
            }
            catch (OperationCanceledException)
            {
                AnsiConsole.MarkupLine("[red]Operation cancelled.[/]");
                return 2; // or another code to indicate cancellation
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                if (settings.Verbose) AnsiConsole.MarkupLine($"[grey]{ex}[/]");
                return 1;
            }
        }
    }

    public class ValidateLicenseSettings : CommonAppSettings
    {
        [CommandOption("-l|--license <PATH>", isRequired: true)]
        [Description("Input license file(s) to validate. Repeat the option to provide multiple files.")]
        public string LicenseFile { get; init; } = default!;

        [CommandOption("-k|--public-key-file <PATH>", isRequired : true)]
        [Description("Path to the RSA public key file.")]
        public string KeyFilePath { get; init; } = default!;
    }

    public sealed class ValidateLincenseCommand : AsyncCommand<ValidateLicenseSettings>
    {
        public override async Task<int> ExecuteAsync(
            [NotNull] CommandContext context, 
            [NotNull] ValidateLicenseSettings settings, 
            CancellationToken cancellationToken)
        {
            AnsiConsole.MarkupLine("[cyan]======= Validating License =======[/]");
            var licenseFile = settings.LicenseFile;
            var keyFile = settings.KeyFilePath;
            if (!File.Exists(licenseFile)) {
                AnsiConsole.MarkupLine($"[yellow]License file '{licenseFile}' — nothing to validate.[/]");
                return 1;
            }
            if (!File.Exists(keyFile)) {
                AnsiConsole.MarkupLine($"[yellow]Public key file '{keyFile}' — cannot validete license {licenseFile}.[/]");
                return 1;
            }
            AnsiConsole.MarkupLine($"[grey]License file :[/] {licenseFile}");
            AnsiConsole.MarkupLine($"[grey]Public key   :[/] {keyFile}");
            AnsiConsole.MarkupLine("");
            cancellationToken.ThrowIfCancellationRequested();
            LicenseVerifier verifier = new(
                publicPemText: File.ReadAllText(keyFile)
            );
            try
            {
                string licenseJson = await File.ReadAllTextAsync(licenseFile, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                bool isValid = verifier.VerifyLicenseJson(licenseJson, out string? failureReason);
                if (isValid)
                {
                    AnsiConsole.MarkupLine("[bold green]✔ License is valid![/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[bold red]✘ License is INVALID![/]");
                    AnsiConsole.MarkupLine($"[red]Reason: {failureReason}[/]");
                }
            }
            catch (OperationCanceledException)
            {
                AnsiConsole.MarkupLine("[red]Operation cancelled.[/]");
                return 2; // or another code to indicate cancellation
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                if (settings.Verbose) AnsiConsole.MarkupLine($"[grey]{ex}[/]");
                return 1;
            }
            return 0;
        }
    }
}