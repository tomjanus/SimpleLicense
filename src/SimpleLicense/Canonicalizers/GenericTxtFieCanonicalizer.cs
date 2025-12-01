/// <summary>
/// Provides generic, format-agnostic canonicalization for plain-text files using 
/// conservative normalization rules that preserve structural and semantic integrity.
/// </summary>
/// <remarks>
/// This canonicalizer applies a set of safe defaults suitable for most text formats:
/// <list type="bullet">
///   <item><description>Normalizes all line endings to <c>\n</c>.</description></item>
///   <item><description>Removes trailing whitespace.</description></item>
///   <item><description>Collapses internal runs of whitespace (including tabs) only in positions 
///        where such transformations do not alter meaning; leading indentation is preserved to 
///        remain compatible with indentation-sensitive formats (e.g., Python, YAML).</description></item>
///   <item><description>Strips full-line comments beginning with common markers (<c>#</c>, <c>;</c>, <c>//</c>).</description></item>
///   <item><description>Trims each line and removes empty lines.</description></item>
///   <item><description>Ensures the output ends with a single trailing newline.</description></item>
/// </list>
/// These operations aim to deliver stable and deterministic text representations while ensuring 
/// that formatting essential to interpretation—particularly indentation—is left intact.
/// </remarks>

using System.Text.RegularExpressions;
using SimpleLicense.Core;

namespace SimpleLicense.Canonicalizers
{
    /// <summary>
    /// Generic fallback canonicalizer for arbitrary plain-text files.
    /// Applies conservative whitespace cleanup, comment stripping,
    /// and normalization that are generally safe across most text formats.
    /// </summary>
    public class TextFileCanonicalizer : IFileCanonicalizer
    {
        // Collapse whitespace runs except at line start
        private static readonly Regex CollapseWhitespaceRe =
            new(@"\s{2,}", RegexOptions.Compiled);

        // Common full-line comment prefixes
        private static readonly string[] FullLineCommentMarkers =
            { "#", ";", "//" };

        private readonly HashSet<string> _supportedExtensions;
        public IEnumerable<string> SupportedExtensions => _supportedExtensions;

        public TextFileCanonicalizer() : this([".txt"]){}

        public TextFileCanonicalizer(IEnumerable<string> extensions)
        {
            // If no extensions provided, use a default set of common text file extensions
            _supportedExtensions = new HashSet<string>(
                extensions.Select(e => e.ToLowerInvariant()),
                StringComparer.OrdinalIgnoreCase
            );
        }

        public string Canonicalize(string input)
        {
            ArgumentNullException.ThrowIfNull(input);

            // Normalize line endings to '\n'
            input = input.Replace("\r\n", "\n").Replace("\r", "\n");
            var lines = input.Split('\n');
            var outLines = new List<string>();

            foreach (var rawLine in lines)
            {
                var line = rawLine;

                // Trim trailing whitespace but DO NOT trim leading whitespace
                // This preserves indentation for YAML, Python, etc.
                line = line.TrimEnd();

                // Skip empty lines after trimming trailing whitespace
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Check for full-line comment
                if (FullLineCommentMarkers.Any(marker => line.TrimStart().StartsWith(marker)))
                    continue;

                // Collapse internal whitespace runs (but preserve leading indentation)
                int leadingSpaces = 0;
                while (leadingSpaces < line.Length && line[leadingSpaces] == ' ')
                    leadingSpaces++;

                string leading = new string(' ', leadingSpaces);
                string body = line.Substring(leadingSpaces);

                // Collapse whitespace inside the body only
                body = CollapseWhitespaceRe.Replace(body, " ");

                line = leading + body;

                outLines.Add(line);
            }

            // Join lines and ensure trailing newline
            var result = string.Join("\n", outLines);
            if (!result.EndsWith("\n"))
                result += "\n";

            return result;
        }
    }
}

