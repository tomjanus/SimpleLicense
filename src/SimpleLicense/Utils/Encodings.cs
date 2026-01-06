using System.Text;

namespace SimpleLicense.Utils
{

    /// <summary>
    /// Maps encoding names to their corresponding Encoding objects
    /// </summary>
    public static class EncodingMap
    {
        private static readonly Dictionary<string, Encoding> _encodings = new(StringComparer.OrdinalIgnoreCase)
        {
            { "utf-8", Encoding.UTF8 },
            { "utf-16", Encoding.Unicode },
            { "utf-16be", Encoding.BigEndianUnicode },
            { "utf-32", Encoding.UTF32 },
            { "ascii", Encoding.ASCII },
            { "latin1", Encoding.Latin1 },
            { "us-ascii", Encoding.ASCII }
        };

        /// <summary>
        /// Get an Encoding object from a string name (case-insensitive)
        /// </summary>
        /// <param name="name">The encoding name (e.g., "utf-8", "ascii")</param>
        /// <returns>The corresponding Encoding object</returns>
        /// <exception cref="ArgumentException">Thrown when the encoding name is not recognized</exception>
        public static Encoding GetEncoding(string name)
        {
            ArgumentNullException.ThrowIfNull(name);
            if (_encodings.TryGetValue(name, out var encoding))
            {
                return encoding;
            }
            throw new ArgumentException($"Unsupported encoding: {name}", nameof(name));
        }

        /// <summary>
        /// Try to get an Encoding object from a string name
        /// </summary>
        /// <param name="name">The encoding name</param>
        /// <param name="encoding">The output encoding if found</param>
        /// <returns>True if the encoding was found, False otherwise</returns>
        public static bool TryGetEncoding(string name, out Encoding? encoding)
        {
            if (name == null){
                encoding = null;
                return false;
            }
            return _encodings.TryGetValue(name, out encoding);
        }
    }
}
