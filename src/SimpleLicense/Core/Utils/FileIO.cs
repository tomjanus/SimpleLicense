/// <summary>
/// Provides file I/O operations with configurable text encoding support.
/// </summary>
/// <remarks>
/// The <see cref="IFileIO"/> interface defines contracts for common file operations including
/// synchronous and asynchronous methods for reading and writing both text and binary data.
/// The <see cref="FileIO"/> class provides a concrete implementation that wraps the standard
/// <see cref="System.IO.File"/> and <see cref="System.IO.Directory"/> operations with support
/// for custom text encoding.
/// </remarks>

using System.Text;

namespace SimpleLicense.Core.Utils
{
    public interface IFileIO
    {
        string ReadAllText(string path);
        Task<string> ReadAllTextAsync(string path, CancellationToken ct = default);

        void WriteAllText(string path, string content);
        Task WriteAllTextAsync(string path, string content, CancellationToken ct = default);

        byte[] ReadAllBytes(string path);
        Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct = default);

        void WriteAllBytes(string path, byte[] data);
        Task WriteAllBytesAsync(string path, byte[] data, CancellationToken ct = default);

        bool FileExists(string path);
        void CreateDirectory(string path);
    }

    public class FileIO : IFileIO
    {
        public Encoding DefaultEncoding { get; }

        public FileIO(Encoding? defaultEncoding = null)
        {
            DefaultEncoding = defaultEncoding ?? Encoding.UTF8;
        }

        public string ReadAllText(string path) =>
            File.ReadAllText(path, DefaultEncoding);

        public Task<string> ReadAllTextAsync(string path, CancellationToken ct = default) =>
            File.ReadAllTextAsync(path, DefaultEncoding, ct);

        public void WriteAllText(string path, string content) =>
            File.WriteAllText(path, content, DefaultEncoding);

        public Task WriteAllTextAsync(string path, string content, CancellationToken ct = default) =>
            File.WriteAllTextAsync(path, content, DefaultEncoding, ct);

        public byte[] ReadAllBytes(string path) =>
            File.ReadAllBytes(path);

        public Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct = default) =>
            File.ReadAllBytesAsync(path, ct);

        public void WriteAllBytes(string path, byte[] data) =>
            File.WriteAllBytes(path, data);

        public Task WriteAllBytesAsync(string path, byte[] data, CancellationToken ct = default) =>
            File.WriteAllBytesAsync(path, data, ct);

        public bool FileExists(string path) =>
            File.Exists(path);

        public void CreateDirectory(string path) =>
            Directory.CreateDirectory(path);
    }
}

