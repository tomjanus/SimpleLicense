using Xunit;
using Shouldly;
using SimpleLicense.Core;
using SimpleLicense.Core.Utils;
using System.Text;
using System.IO;

namespace SimpleLicense.Tests
{
    /// <summary>
    /// Tests for TextFileHasher - SHA-256 hashing utilities
    /// </summary>
    public class HashingTests
    {
        [Fact]
        public void Sha256Hex_WithSimpleString_ShouldReturnCorrectHash()
        {
            // Arrange
            var input = "hello world";
            var expectedHash = "b94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9";

            // Act
            var result = TextFileHasher.Sha256Hex(input);

            // Assert
            result.ShouldBe(expectedHash);
            result.ShouldBeOfType<string>();
            result.Length.ShouldBe(64); // SHA-256 produces 64 hex characters
        }

        [Fact]
        public void Sha256Hex_WithEmptyString_ShouldReturnCorrectHash()
        {
            // Arrange
            var input = "";
            var expectedHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

            // Act
            var result = TextFileHasher.Sha256Hex(input);

            // Assert
            result.ShouldBe(expectedHash);
        }

        [Fact]
        public void Sha256Hex_WithNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            string? input = null;

            // Act & Assert
            Should.Throw<ArgumentNullException>(() => TextFileHasher.Sha256Hex(input!));
        }

        [Fact]
        public void Sha256Hex_WithUtf8Encoding_ShouldReturnCorrectHash()
        {
            // Arrange
            var input = "hello 世界";
            
            // Act
            var result = TextFileHasher.Sha256Hex(input, Encoding.UTF8);

            // Assert
            result.ShouldNotBeNullOrEmpty();
            result.Length.ShouldBe(64);
        }

        [Fact]
        public void Sha256Hex_WithDifferentEncodings_ShouldReturnDifferentHashes()
        {
            // Arrange
            var input = "hello";

            // Act
            var utf8Hash = TextFileHasher.Sha256Hex(input, Encoding.UTF8);
            var asciiHash = TextFileHasher.Sha256Hex(input, Encoding.ASCII);
            var utf32Hash = TextFileHasher.Sha256Hex(input, Encoding.UTF32);

            // Assert - for ASCII-compatible strings, UTF8 and ASCII should be the same
            utf8Hash.ShouldBe(asciiHash);
            // But UTF32 uses different byte representation
            utf32Hash.ShouldNotBe(utf8Hash);
        }

        [Fact]
        public void Sha256Hex_ShouldReturnLowercaseHexString()
        {
            // Arrange
            var input = "TEST";

            // Act
            var result = TextFileHasher.Sha256Hex(input);

            // Assert
            result.ShouldBe(result.ToLower());
            result.ShouldMatch("^[0-9a-f]{64}$"); // Only lowercase hex digits
        }

        [Fact]
        public void HashFile_WithSimpleTextFile_ShouldReturnCorrectHash()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var content = "hello world";
            File.WriteAllText(tempFile, content, Encoding.UTF8);
            var expectedHash = TextFileHasher.Sha256Hex(content);

            try
            {
                // Act
                var result = TextFileHasher.HashFile(tempFile, encodingName: "utf-8");

                // Assert
                result.ShouldBe(expectedHash);
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public void HashFile_WithMultilineContent_ShouldReturnCorrectHash()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var content = "line1\nline2\nline3";
            File.WriteAllText(tempFile, content, Encoding.UTF8);
            var expectedHash = TextFileHasher.Sha256Hex(content);

            try
            {
                // Act
                var result = TextFileHasher.HashFile(tempFile);

                // Assert
                result.ShouldBe(expectedHash);
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public void HashFile_WithDifferentEncodings_ShouldRespectEncodingParameter()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var content = "hello";
            File.WriteAllText(tempFile, content, Encoding.UTF8);

            try
            {
                // Act
                var utf8Hash = TextFileHasher.HashFile(tempFile, encodingName: "utf-8");
                var asciiHash = TextFileHasher.HashFile(tempFile, encodingName: "ascii");

                // Assert
                utf8Hash.ShouldBe(asciiHash); // For ASCII-compatible content
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public void HashFile_WithNullPath_ShouldThrowArgumentNullException()
        {
            // Arrange
            string? path = null;

            // Act & Assert
            Should.Throw<ArgumentNullException>(() => TextFileHasher.HashFile(path!));
        }

        [Fact]
        public void HashFile_WithNonExistentFile_ShouldThrowFileNotFoundException()
        {
            // Arrange
            var nonExistentPath = Path.Combine(Path.GetTempPath(), "nonexistent_file_12345.txt");

            // Act & Assert
            Should.Throw<FileNotFoundException>(() => TextFileHasher.HashFile(nonExistentPath));
        }

        [Fact]
        public void HashFile_WithCanonicalizer_ShouldApplyCanonicalization()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var content = "  hello  \n  world  ";
            File.WriteAllText(tempFile, content, Encoding.UTF8);
            
            var canonicalizer = new TestCanonicalizer();
            var expectedCanonicalContent = canonicalizer.Canonicalize(content);
            var expectedHash = TextFileHasher.Sha256Hex(expectedCanonicalContent);

            try
            {
                // Act
                var result = TextFileHasher.HashFile(tempFile, canonicalizer);

                // Assert
                result.ShouldBe(expectedHash);
                result.ShouldNotBe(TextFileHasher.Sha256Hex(content)); // Should differ from non-canonical hash
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public void HashFile_WithNullCanonicalizer_ShouldHashRawContent()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var content = "test content";
            File.WriteAllText(tempFile, content, Encoding.UTF8);
            var expectedHash = TextFileHasher.Sha256Hex(content);

            try
            {
                // Act
                var result = TextFileHasher.HashFile(tempFile, canonicalizer: null);

                // Assert
                result.ShouldBe(expectedHash);
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public void HashFile_SameContentDifferentFiles_ShouldReturnSameHash()
        {
            // Arrange
            var tempFile1 = Path.GetTempFileName();
            var tempFile2 = Path.GetTempFileName();
            var content = "identical content";
            File.WriteAllText(tempFile1, content, Encoding.UTF8);
            File.WriteAllText(tempFile2, content, Encoding.UTF8);

            try
            {
                // Act
                var hash1 = TextFileHasher.HashFile(tempFile1);
                var hash2 = TextFileHasher.HashFile(tempFile2);

                // Assert
                hash1.ShouldBe(hash2);
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile1))
                    File.Delete(tempFile1);
                if (File.Exists(tempFile2))
                    File.Delete(tempFile2);
            }
        }

        [Fact]
        public void HashFile_DifferentContent_ShouldReturnDifferentHashes()
        {
            // Arrange
            var tempFile1 = Path.GetTempFileName();
            var tempFile2 = Path.GetTempFileName();
            File.WriteAllText(tempFile1, "content A", Encoding.UTF8);
            File.WriteAllText(tempFile2, "content B", Encoding.UTF8);

            try
            {
                // Act
                var hash1 = TextFileHasher.HashFile(tempFile1);
                var hash2 = TextFileHasher.HashFile(tempFile2);

                // Assert
                hash1.ShouldNotBe(hash2);
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile1))
                    File.Delete(tempFile1);
                if (File.Exists(tempFile2))
                    File.Delete(tempFile2);
            }
        }

        /// <summary>
        /// Test canonicalizer that trims whitespace from each line
        /// </summary>
        private class TestCanonicalizer : IFileCanonicalizer
        {
            public IEnumerable<string> SupportedExtensions => new[] { ".txt" };

            public string Canonicalize(string content)
            {
                var lines = content.Split('\n');
                return string.Join("\n", lines.Select(line => line.Trim()));
            }
        }
    }
}
