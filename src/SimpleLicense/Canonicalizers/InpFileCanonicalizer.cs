/// <summary>
/// Canonicalizer for EPANET .inp files
/// </summary>

using System.Text.RegularExpressions;
using SimpleLicense.Core;

namespace SimpleLicense.Canonicalizers
{
    public class InpFileCanonicalizer : IFileCanonicalizer
    {
        private static readonly Regex SectionHeaderRe = new(@"^\s*\[(?<name>[^\]]+)\]\s*$", RegexOptions.Compiled);
        private static readonly Regex CollapseWhitespaceRe = new(@"\s+", RegexOptions.Compiled);
        
        private static readonly string[] DefaultExtensions = [".inp"];

        private readonly HashSet<string> _supportedExtensions;
        public IEnumerable<string> SupportedExtensions => _supportedExtensions;
        
        public InpFileCanonicalizer() : this([".inp"]){}

        public InpFileCanonicalizer(IEnumerable<string>? extensions)
        {
            // If no extensions provided, use a default set of common text file extensions
            var extList = (extensions == null || !extensions.Any()) 
                ? DefaultExtensions 
                : extensions;
            _supportedExtensions = new HashSet<string>(
                extList.Select(e => e.ToLowerInvariant()),
                StringComparer.OrdinalIgnoreCase
            );
        }

        /// <summary>
        /// Canonicalize the contents of an EPANET .inp file according to rules:
        /// - normalize line endings to '\n'
        /// - remove the entire [TITLE] section
        /// - strip comments that start with ';' (both full-line and inline)
        /// - trim lines, collapse any run of whitespace to single space
        /// - uppercase section headers and keep them as a single token on their own line
        /// - drop empty lines
        /// - always end with a single '\n'
        /// </summary>
        /// <param name="input">The raw INP file content as a string</param>
        /// <returns>A canonicalized version of the input text with normalized formatting and comments removed</returns>
        public string Canonicalize(string input)
        {
            ArgumentNullException.ThrowIfNull(input);
            // Normalize line endings
            input = input.Replace("\r\n", "\n").Replace("\r", "\n");
            var outLines = new List<string>();
            bool inTitleSection = false;
            var lines = input.Split('\n');
            foreach (var raw in lines)
            {
                var line = raw;
                // Detect section header
                var m = SectionHeaderRe.Match(line);
                if (m.Success)
                {
                    var section = m.Groups["name"].Value.Trim();
                    var sectionUpper = section.ToUpperInvariant();
                    // If we encountered the end of TITLE section
                    if (inTitleSection)
                    {
                        // any new section header ends the title section
                        inTitleSection = false;
                    }
                    if (sectionUpper == "TITLE")
                    {
                        // enter TITLE section and skip the header itself
                        inTitleSection = true;
                        continue;
                    }
                    else
                    {
                        // emit normalized header: [SECTIONNAME]
                        outLines.Add($"[{sectionUpper}]");
                        continue;
                    }
                }
                if (inTitleSection)
                    continue; // skip any lines that are inside [TITLE] until next section header
                // Strip inline comments: anything after ';' is a comment
                var semIndex = line.IndexOf(';');
                if (semIndex >= 0)
                    line = line[..semIndex];
                // Trim whitespace
                line = line.Trim();
                if (string.IsNullOrEmpty(line))
                    continue; // skip empty lines
                // Collapse any sequence of whitespace into a single space.
                // This preserves single-space internal separators inside field values (e.g. "HEAD 1")
                line = CollapseWhitespaceRe.Replace(line, " ");
                outLines.Add(line);
            }
            // Join with '\n' and ensure exactly one trailing newline
            var result = string.Join("\n", outLines);
            if (!result.EndsWith("\n")) result += "\n";
            return result;
        }
    }  
}