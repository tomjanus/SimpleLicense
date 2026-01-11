/// <summary>
/// Field processors for license creation.
/// 
/// PROCESSOR ARCHITECTURE:
/// =======================
/// Field processors execute transformations on field values during license creation.
/// They run BEFORE validation and serialization.
/// 
/// PURPOSE:
/// - Transform input values into final license field values
/// - Compute derived data (e.g., file hashes from file paths)
/// - Apply business logic during license generation
/// - Enable custom per-field processing logic
/// 
/// EXAMPLE USE CASES:
/// - Files field: Read file paths, compute SHA256 hashes
/// - Signature field: Generate digital signature from license data
/// - Metadata field: Auto-populate system information
/// - Custom computed fields: Apply domain-specific transformations
/// 
/// EXECUTION FLOW:
/// 1. User provides raw input (e.g., ["file1.txt", "file2.txt"])
/// 2. Processor transforms input (e.g., compute hashes)
/// 3. Result stored in license field
/// 4. Field validator validates the processed value
/// 5. Field serializer formats for JSON output
/// 
/// EXTENSIBILITY:
/// - Register custom processors for application-specific fields
/// - Processors can accept configuration parameters
/// - Chain multiple transformations if needed
/// </summary>

using System.Security.Cryptography;

namespace SimpleLicense.Core.LicenseValidation
{
    /// <summary>
    /// Attribute to mark a method as a field processor that should be auto-discovered.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class FieldProcessorAttribute : Attribute
    {
        /// <summary>
        /// The name of the processor. Used to reference the processor in schemas.
        /// </summary>
        public string ProcessorName { get; }

        public FieldProcessorAttribute(string processorName)
        {
            ProcessorName = processorName;
        }
    }

    /// <summary>
    /// Context passed to field processors containing additional information.
    /// </summary>
    public class ProcessorContext
    {
        /// <summary>
        /// The name of the field being processed.
        /// </summary>
        public string FieldName { get; init; } = string.Empty;
        
        /// <summary>
        /// The field descriptor from the schema.
        /// </summary>
        public FieldDescriptor? FieldDescriptor { get; init; }
        
        /// <summary>
        /// Optional configuration parameters for the processor.
        /// </summary>
        public Dictionary<string, object> Parameters { get; init; } = new();

        /// <summary>
        /// Working directory for file operations (if applicable).
        /// </summary>
        public string? WorkingDirectory { get; init; }
    }

    /// <summary>
    /// Delegate type for field processors.
    /// Takes a raw input value and context, returns the processed value.
    /// </summary>
    public delegate object? FieldProcessor(object? value, ProcessorContext context);

    /// <summary>
    /// Registry of field processors with auto-discovery support.
    /// Provides built-in processors for common operations and allows custom processor registration.
    /// </summary>
    public static class FieldProcessors
    {
        private static readonly Dictionary<string, FieldProcessor> _processors = new(StringComparer.OrdinalIgnoreCase);
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets all registered processors. Initializes built-in processors on first access.
        /// </summary>
        public static IReadOnlyDictionary<string, FieldProcessor> All
        {
            get
            {
                EnsureInitialized();
                return _processors;
            }
        }

        /// <summary>
        /// Registers a processor with the specified name.
        /// </summary>
        public static void Register(string processorName, FieldProcessor processor)
        {
            ArgumentNullException.ThrowIfNull(processorName);
            ArgumentNullException.ThrowIfNull(processor);
            
            lock (_lock)
            {
                _processors[processorName] = processor;
            }
        }

        /// <summary>
        /// Gets a processor by name, or null if not registered.
        /// </summary>
        public static FieldProcessor? Get(string processorName)
        {
            EnsureInitialized();
            _processors.TryGetValue(processorName, out var processor);
            return processor;
        }

        /// <summary>
        /// Checks if a processor is registered.
        /// </summary>
        public static bool IsRegistered(string processorName)
        {
            EnsureInitialized();
            return _processors.ContainsKey(processorName);
        }

        /// <summary>
        /// Tries to get a processor by name, returning a boolean indicating success.
        /// </summary>
        public static bool TryGet(string processorName, out FieldProcessor? processor)
        {
            EnsureInitialized();
            return _processors.TryGetValue(processorName, out processor);
        }

        /// <summary>
        /// Removes a processor registration.
        /// </summary>
        public static bool Unregister(string processorName)
        {
            lock (_lock)
            {
                return _processors.Remove(processorName);
            }
        }

        private static void EnsureInitialized()
        {
            if (_initialized) return;
            
            lock (_lock)
            {
                if (_initialized) return;
                
                // Auto-discover processors with [FieldProcessor] attribute
                var methods = typeof(FieldProcessors).GetMethods(
                    System.Reflection.BindingFlags.Static | 
                    System.Reflection.BindingFlags.Public | 
                    System.Reflection.BindingFlags.NonPublic);
                
                foreach (var method in methods)
                {
                    var attr = method.GetCustomAttributes(typeof(FieldProcessorAttribute), false)
                        .Cast<FieldProcessorAttribute>()
                        .FirstOrDefault();
                    
                    if (attr != null)
                    {
                        var processor = (FieldProcessor)Delegate.CreateDelegate(typeof(FieldProcessor), method);
                        _processors[attr.ProcessorName] = processor;
                    }
                }
                
                _initialized = true;
            }
        }

        // ============================================================
        // BUILT-IN PROCESSORS
        // ============================================================

        /// <summary>
        /// Computes SHA256 hashes for a list of file paths.
        /// Input: List of file paths (string[] or List&lt;string&gt;)
        /// Output: Dictionary mapping file paths to their SHA256 hashes
        /// </summary>
        [FieldProcessor("HashFiles")]
        public static object? HashFilesProcessor(object? value, ProcessorContext context)
        {
            if (value == null) return null;

            var filePaths = value switch
            {
                string[] arr => arr,
                List<string> list => list.ToArray(),
                IEnumerable<string> enumerable => enumerable.ToArray(),
                string single => new[] { single },
                _ => throw new ArgumentException($"HashFiles processor expects string[] or List<string>, got {value.GetType()}")
            };

            var workingDir = context.WorkingDirectory ?? Directory.GetCurrentDirectory();
            var result = new Dictionary<string, string>();

            foreach (var filePath in filePaths)
            {
                var fullPath = Path.IsPathRooted(filePath) 
                    ? filePath 
                    : Path.Combine(workingDir, filePath);

                if (!File.Exists(fullPath))
                    throw new FileNotFoundException($"File not found for hashing: {fullPath}");

                using var stream = File.OpenRead(fullPath);
                var hashBytes = SHA256.HashData(stream);
                var hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();
                
                result[filePath] = hashString; // Use original path as key
            }

            return result;
        }

        /// <summary>
        /// Computes SHA256 hash for a single file.
        /// Input: File path (string)
        /// Output: SHA256 hash (string)
        /// </summary>
        [FieldProcessor("HashFile")]
        public static object? HashFileProcessor(object? value, ProcessorContext context)
        {
            if (value == null) return null;
            
            if (value is not string filePath)
                throw new ArgumentException($"HashFile processor expects string, got {value.GetType()}");

            var workingDir = context.WorkingDirectory ?? Directory.GetCurrentDirectory();
            var fullPath = Path.IsPathRooted(filePath) 
                ? filePath 
                : Path.Combine(workingDir, filePath);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"File not found for hashing: {fullPath}");

            using var stream = File.OpenRead(fullPath);
            var hashBytes = SHA256.HashData(stream);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        /// <summary>
        /// Passes through the value unchanged.
        /// Useful as a placeholder or for explicit "no processing" declaration.
        /// </summary>
        [FieldProcessor("PassThrough")]
        public static object? PassThroughProcessor(object? value, ProcessorContext context)
        {
            return value;
        }

        /// <summary>
        /// Converts value to uppercase string.
        /// </summary>
        [FieldProcessor("ToUpper")]
        public static object? ToUpperProcessor(object? value, ProcessorContext context)
        {
            return value?.ToString()?.ToUpperInvariant();
        }

        /// <summary>
        /// Converts value to lowercase string.
        /// </summary>
        [FieldProcessor("ToLower")]
        public static object? ToLowerProcessor(object? value, ProcessorContext context)
        {
            return value?.ToString()?.ToLowerInvariant();
        }

        /// <summary>
        /// Generates a GUID.
        /// Ignores input value.
        /// </summary>
        [FieldProcessor("GenerateGuid")]
        public static object? GenerateGuidProcessor(object? value, ProcessorContext context)
        {
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Sets current UTC timestamp.
        /// Ignores input value.
        /// </summary>
        [FieldProcessor("CurrentTimestamp")]
        public static object? CurrentTimestampProcessor(object? value, ProcessorContext context)
        {
            return DateTime.UtcNow;
        }
    }
}
