namespace SimpleLicense.Core
{
    public interface IFileCanonicalizer
    {
        /// <summary>
        /// Canonicalizes the raw content of a file.
        /// </summary>
        /// <param name="input">Raw file content</param>
        /// <returns>Canonicalized content</returns>
        string Canonicalize(string input);

        /// <summary>
        /// Optionally, specify file extensions this canonicalizer supports (e.g. ".inp")
        /// </summary>
        IEnumerable<string> SupportedExtensions { get; }
    }
}