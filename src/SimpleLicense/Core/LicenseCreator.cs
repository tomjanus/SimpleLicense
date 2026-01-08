// computes hashes & builds license

using System.Text;
using SimpleLicense.Canonicalizers;
using SimpleLicense.LicenseValidation;

namespace SimpleLicense.Core
{

    public interface IFileSource
    {
        /// <summary>Enumerate full file paths. 
        /// Implementation controls recursion/filtering.</summary>
        IEnumerable<string> EnumerateFiles();
    }

    public class FolderFileSource : IFileSource
    {
        public string Folder { get; }
        public string Pattern { get; }
        public bool Recursive { get; }

        public FolderFileSource(string folder, string pattern = "*.*", bool recursive = true)
        {
            Folder = folder ?? throw new ArgumentNullException(nameof(folder));
            if (!Directory.Exists(Folder))
                throw new DirectoryNotFoundException($"The folder '{Folder}' does not exist.");
                    // Validate pattern argument
            if (string.IsNullOrWhiteSpace(pattern))
                throw new ArgumentException("Pattern cannot be null or whitespace.", nameof(pattern));
            Pattern = pattern;
            Recursive = recursive;
        }

        public IEnumerable<string> EnumerateFiles()
        {
            foreach (var f in Directory.EnumerateFiles(Folder, Pattern, Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                yield return Path.GetFullPath(f);
        }
    }

    public class ListFileSource : IFileSource
    {
        public IEnumerable<string>? Paths { get; }
        public ListFileSource(IEnumerable<string>? paths = null)
        {
            Paths = paths;
        }
        public IEnumerable<string> EnumerateFiles()
        {
            // If Paths is null or empty, return an empty enumerable
            return Paths?.Select(Path.GetFullPath) ?? Enumerable.Empty<string>();
        }
    }

    public class LicenseCreator
    {
        // usage: SimpleLicense.Core.LicenseCreator creator = new SimpleLicense.Core.LicenseCreator();
        //        creator.OnInfo += msg => Console.WriteLine(msg);
        //        var license = creator.CreateLicenseDocument(...);
        public string? LicenseId { get; init; }
        public int? NumberOfValidMonths {get; set; }
        public int? MaxJunctions { get; set; }
        public string? LicenseInfo { get; set; }
        public IEnumerable<string>? InputFiles { get; set; }
        
        /// <summary>
        /// Optional custom fields to include in the license.
        /// Keys are field names, values are field values (can be any JSON-serializable type).
        /// </summary>
        public Dictionary<string, object?>? CustomFields { get; set; }

        // Event-based logging or info callbacks (nullable delegate/event)
        public event Action<string>? OnInfo;
        private void Info(string message) => OnInfo?.Invoke(message);

        // Dependency-injected file I/O abstraction for saving/loading files
        private readonly IFileIO _fileIO;

        public LicenseCreator(string? licenseID = null, IFileIO? fileIO = null)
        {
            _fileIO = fileIO ?? new FileIO();
            LicenseId = licenseID ?? Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Creates a flexible LicenseDocument with mandatory and optional custom fields.
        /// </summary>
        /// <param name="fileSource">Optional source for files to hash and include in license</param>
        /// <param name="canonicalizer">Optional canonicalizer for file hashing</param>
        /// <param name="encodingName">Text encoding to use when reading files</param>
        /// <returns>A new LicenseDocument instance</returns>
        public LicenseDocument CreateLicenseDocument(
            IFileSource? fileSource = null, 
            IFileCanonicalizer? canonicalizer = null,
            string encodingName = "utf-8")
        {
            var doc = new LicenseDocument(ensureMandatoryKeys: true);
            
            // Set mandatory fields
            doc["LicenseId"] = LicenseId ?? Guid.NewGuid().ToString();
            
            DateTime? expiryUtc = NumberOfValidMonths is int numMonths 
                ? DateTime.UtcNow.AddMonths(numMonths) 
                : null;
            doc["ExpiryUtc"] = expiryUtc;
            
            // Will be filled later by signing
            doc["Signature"] = null;
            
            // Set optional standard fields
            if (LicenseInfo != null)
                doc["LicenseInfo"] = LicenseInfo;
            
            if (MaxJunctions.HasValue)
                doc["MaxJunctions"] = MaxJunctions.Value;
            
            // Add custom fields
            if (CustomFields != null)
            {
                foreach (var kvp in CustomFields)
                {
                    if (!doc.SetField(kvp.Key, kvp.Value, out var error))
                    {
                        Info($"Warning: Custom field '{kvp.Key}' validation failed: {error}");
                    }
                }
            }
            
            // Process file hashes if provided
            var files = fileSource?.EnumerateFiles().ToList() 
                       ?? InputFiles?.ToList() 
                       ?? new List<string>();
            
            if (files.Count > 0)
            {
                var fileHashes = new List<string>();
                foreach (var filePath in files)
                {
                    if (!_fileIO.FileExists(filePath))
                    {
                        Info($"Input file could not be found at {filePath}. Skipping.");
                        continue;
                    }
                    
                    var fileHash = TextFileHasher.HashFile(
                        filePath, 
                        canonicalizer, 
                        encodingName);
                    fileHashes.Add(fileHash);
                    Info($"Computed hash for file '{filePath}': {fileHash}");
                }
                
                if (fileHashes.Count > 0)
                    doc["AllowedFileHashes"] = fileHashes;
            }
            
            return doc;
        }
    }
}