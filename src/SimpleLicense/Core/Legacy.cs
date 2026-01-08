using System.Text;
using SimpleLicense.Canonicalizers;

namespace SimpleLicense.Core
{
    /// <summary>
    /// Legacy License record for backwards compatibility.
    /// For new code, use LicenseDocument which provides more flexibility.
    /// </summary>
    public sealed record License(
        string LicenseId,
        string LicenseInfo = "Standard Styx License",
        DateTime? ExpiryUtc = null,
        int? MaxJunctions = null,
        List<string>? AllowedFileHashes = null,
        string? Signature = null  // Will hold the cryptographic signature
    );

    public class LicenseCreatorLegacy
    {

        public string? LicenseId { get; init; }
        public int? NumberOfValidMonths {get; set; }
        public int? MaxJunctions { get; set; }
        public string? LicenseInfo { get; set; }
        public IEnumerable<string>? InputFiles { get; set; }

        // Event-based logging or info callbacks (nullable delegate/event)
        public event Action<string>? OnInfo;
        private void Info(string message) => OnInfo?.Invoke(message);

        // Dependency-injected file I/O abstraction for saving/loading files
        private readonly IFileIO _fileIO;
        
        public LicenseCreatorLegacy(string? licenseID = null, IFileIO? fileIO = null)
        {
            _fileIO = fileIO ?? new FileIO();
            LicenseId = licenseID ?? Guid.NewGuid().ToString();
        }

        // Implementation of legacy license creation methods would go here
        /// <summary>
        /// Legacy method for backwards compatibility. Creates a rigid License record.
        /// For new code, use CreateLicenseDocument() instead.
        /// </summary>
        [Obsolete("Use CreateLicenseDocument() for more flexibility")]
        public License CreateLicense(string encodingName = "utf-8")
        {
            DateTime? expiryUtc = NumberOfValidMonths is int numMonths 
                ? DateTime.UtcNow.AddMonths(numMonths) 
                : null;
            int? maxJunctions = MaxJunctions;
            var inpFileHashes = new List<string>();
            foreach (var inpFilePath in InputFiles ?? Array.Empty<string>())
            {
                if (!_fileIO.FileExists(inpFilePath))
                {
                    Info($"Input file could not be found at {inpFilePath}. Skipping.");
                    continue;
                }
                else
                {
                    var fileHash = TextFileHasher.HashFile(
                        inpFilePath, new InpFileCanonicalizer(), encodingName);
                    inpFileHashes.Add(fileHash);
                    Info($"Computed hash for INP file '{inpFilePath}': {fileHash}");

                }
            }
            License license = new License(
                LicenseId: LicenseId ?? Guid.NewGuid().ToString(),
                LicenseInfo: LicenseInfo ?? "Standard Styx License",
                ExpiryUtc: expiryUtc,
                MaxJunctions: maxJunctions,
                AllowedFileHashes: inpFileHashes.Count > 0 ? [.. inpFileHashes] : null
            );
            return license;
        }
    }
}