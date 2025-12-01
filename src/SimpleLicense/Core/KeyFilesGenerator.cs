// usage: Styx.License.KeyFilesGenerator keyGen = new Styx.License.KeyGenerator(3072);
//        keyGen.OnInfo += msg => Console.WriteLine(msg);
//        var result = keyGen.GenerateKeys();
using System.Security.Cryptography;
using System.Text;

namespace SimpleLicense.Core
{
    public class KeyGenerationResult
    {
        public required string PrivateKeyPath { get; init; }
        public required string PublicKeyPath { get; init; }
        public required bool Success { get; init; }
    }
    internal class KeyGenerator
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

        public KeyGenerator(int keySize = 2048)
        {
            if (keySize < 2048)
                throw new ArgumentException("RSA key size should be at least 2048 bits.", nameof(keySize));
            KeySize = keySize;
        }

        /// <summary>
        /// Generates an RSA key pair using the configured KeySize.
        /// </summary>
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