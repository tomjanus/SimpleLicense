// usage: Styx.License.KeyFilesGenerator keyGen = new Styx.License.KeyGenerator(3072);
//        keyGen.OnInfo += msg => Console.WriteLine(msg);
//        var result = keyGen.GenerateKeys();
using System.Security.Cryptography;
using System.Text;

namespace SimpleLicense.Core.Cryptography
{
    /// <summary>
    /// Result of key generation process.
    /// </summary>
    /// <args>
    /// - PrivateKeyPath: Path to the generated private key PEM file
    /// - PublicKeyPath: Path to the generated public key PEM file
    /// - Success: Indicates if key generation was successful
    /// </args>
    public class KeyGenerationResult
    {
        public required string PrivateKeyPath { get; init; }
        public required string PublicKeyPath { get; init; }
        public required bool Success { get; init; }
    }

    /// <summary>
    /// Generates RSA key pairs and saves them as PEM files.
    /// </summary>
    /// <args>
    /// - KeySize: RSA key size in bits (default 2048; 3072+ recommended)
    /// - KeyDir: Directory to save generated keys into (default current directory)
    /// </args>
    /// <events>
    /// - OnInfo: Event triggered with informational messages during key generation
    /// </events>
    public class KeyGenerator
    {
        public event Action<string>? OnInfo;
        private void Info(string message) => OnInfo?.Invoke(message);

        /// <summary>
        /// RSA key size in bits.
        /// Default is 2048; 3072+ recommended.
        /// </summary>
        public int KeySize { get; }

        /// <summary>
        /// Directory to save generated keys into.
        /// </summary>
        public DirectoryInfo KeyDir { get; set; } = new DirectoryInfo(".");

        /// <summary>
        /// Initializes a new instance of KeyGenerator with specified key size.
        /// </summary>
        /// <param name="keySize">RSA key size in bits (default 2048; 3072+ recommended)</param>
        public KeyGenerator(int keySize = 2048)
        {
            if (keySize < 2048)
                throw new ArgumentException("RSA key size should be at least 2048 bits.", nameof(keySize));
            KeySize = keySize;
        }

        /// <summary>
        /// Generates an RSA key pair using the configured KeySize.
        /// </summary>
        /// <param name="keyName">Optional base name for the key files. If not provided, a name based on key size and date will be used.</param>
        /// <returns>KeyGenerationResult containing paths to the generated key files and success status.</
        public KeyGenerationResult GenerateKeys(string? keyName = null)
        {
            if (!KeyDir.Exists)
            {
                KeyDir.Create();
                Info($"Directory created: {KeyDir.FullName}");
            }

            keyName ??= $"key_{KeySize}bit_{DateTime.UtcNow:yyyyMMdd}";
            
            var privateKeyPath = Path.Combine(KeyDir.FullName, $"{keyName}_private.pem");
            var publicKeyPath = Path.Combine(KeyDir.FullName, $"{keyName}_public.pem");

            using var rsa = RSA.Create(KeySize);
            var pkcs8 = rsa.ExportPkcs8PrivateKey();
            var spki = rsa.ExportSubjectPublicKeyInfo();

            File.WriteAllText(privateKeyPath, Pem("PRIVATE KEY", pkcs8));
            File.WriteAllText(publicKeyPath, Pem("PUBLIC KEY", spki));
            Info($"Private key saved to: {privateKeyPath}");
            Info($"Public key saved to: {publicKeyPath}");

            return new KeyGenerationResult
            {
                PrivateKeyPath = privateKeyPath,
                PublicKeyPath = publicKeyPath,
                Success = true
            };
        }

        // PEM ("Privacy Enhanced Mail") is a Base64 text encoding used for cryptographic keys.
        // It wraps base64-encoded DER data with header/footer lines.
        // A PEM file for a private key might look like:
        // -----BEGIN PRIVATE KEY-----
        // MIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQD...
        // -----END PRIVATE KEY-----
        /// <summary>
        /// Converts binary key data to PEM format with appropriate headers and footers.
        /// </summary>
        /// <param name="label">Label for the PEM block (e.g., "PRIVATE KEY", "PUBLIC KEY")</param>
        /// <param name="data">Binary key data to encode</param>
        /// <returns>PEM-formatted string</returns>
        static string Pem(string label, byte[] data)
        {
            string b64 = Convert.ToBase64String(data);
            var sb = new StringBuilder();
            sb.AppendLine($"-----BEGIN {label}-----");
            for (int i=0; i < b64.Length; i+=64)
                sb.AppendLine(b64.Substring(i, Math.Min(64, b64.Length - i)));
            sb.AppendLine($"-----END {label}-----");
            return sb.ToString();
        }
    }
}