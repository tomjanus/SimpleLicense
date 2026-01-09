using System.Security.Cryptography;
using System.Text;

namespace SimpleLicense.Core.Utils
{
    public static class TextFileHasher
    {

        /// <summary>
        ///  Compute SHA-256 hash of input string and return as lowercase hex string
        /// </summary>
        /// <param name="input"></param>
        /// <returns>Lowercase hex string representing the SHA-256 hash of the input</returns>
        public static string Sha256Hex(string input, Encoding? encoding = null)
        {
            ArgumentNullException.ThrowIfNull(input);
            encoding ??= Encoding.UTF8;
            var bytes = encoding.GetBytes(input);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexStringLower(hash);
        }

        /// <summary>
        /// Compute SHA-256 hash of a text file, with optional canonicalization, and return
        /// as lowercase hex string
        /// </summary>
        /// <param name="path">Path to the text file to hash</param>
        /// <param name="canonicalizer">Optional canonicalizer to process the file content before hashing</param>
        /// <param name="encodingName">Name of the text encoding to use when reading the file (default "utf-8")</param>
        /// <returns>
        /// Lowercase hex string representing the SHA-256 hash of the (optionally canonicalized) file content
        /// </returns>
        public static string HashFile(
            string path,
            IFileCanonicalizer? canonicalizer = null,
            string encodingName = "utf-8")
        {
            var encoding = EncodingMap.GetEncoding(encodingName);
            ArgumentNullException.ThrowIfNull(path);
            var rawText = File.ReadAllText(path, encoding);
            string? canonicalText;
            if (canonicalizer != null)
            {
                canonicalText = canonicalizer.Canonicalize(rawText);
                return Sha256Hex(canonicalText, encoding);
            }
            canonicalText = rawText;
            return Sha256Hex(canonicalText, encoding);
        }

    }
}
