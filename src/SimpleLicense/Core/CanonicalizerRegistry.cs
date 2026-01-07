using System.Text.Json;

namespace SimpleLicense.Core
{

    /// <summary>
    /// Registry for file canonicalizers.
    /// </summary>
    /// <remarks>
    /// Example:
    /// <code>
    /// CanonicalizerRegistry.LoadFromJson("canonicalizers.json");
    /// var can = CanonicalizerRegistry.GetCanonicalizer(".txt");
    /// </code>
    /// </remarks>
    public static class CanonicalizerRegistry
    {
        private static readonly Dictionary<string, IFileCanonicalizer> _registry = new();

        public static void Register(IFileCanonicalizer canonicalizer)
        {
            foreach (var ext in canonicalizer.SupportedExtensions)
            {
                _registry[ext.ToLowerInvariant()] = canonicalizer;
            }
        }

        public static IFileCanonicalizer? GetCanonicalizer(string extension)
        {
            _registry.TryGetValue(extension.ToLowerInvariant(), out var canonicalizer);
            return canonicalizer;
        }

        public static List<string> GetRegisteredExtensions()
        {
            return _registry.Keys.ToList();
        }

        public static IDictionary<string, IFileCanonicalizer> Registry => _registry;

        /// <summary>
        /// Loads and registers canonicalizers from a JSON configuration file
        /// JSON format: { ".ext": "Namespace.TypeName", ... }
        /// </summary>
        public static void LoadFromJson(string jsonPath)
        {
            var json = File.ReadAllText(jsonPath);
            var config = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json) 
                ?? throw new InvalidOperationException("Could not parse canonicalizer config JSON.");

            foreach (var (typeName, extensions) in config)
            {
                var type = Type.GetType(typeName) 
                    ?? throw new InvalidOperationException($"Type '{typeName}' not found.");
                    
                var canonicalizer = (IFileCanonicalizer)Activator.CreateInstance(type, new object[] { extensions })!;
                foreach (var extension in extensions)
                    _registry[extension.ToLowerInvariant()] = canonicalizer;
            }
        }

    }
}
