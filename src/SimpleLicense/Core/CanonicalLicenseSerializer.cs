//
using System.Text;
using System.Text.Json;
using SimpleLicense.LicenseValidation;

namespace SimpleLicense.Core
{

    public class CanonicalLicenseSerializer : LicenseSerializer
    {
        public List<string> UnSerializedFields { get; init; } = [];
        
        public CanonicalLicenseSerializer() : base(JsonProfiles.Canonical) {}

        public new byte[] Serialize(License license)
        {
            return GetCanonicalUtf8BytesFromObject(license, UnSerializedFields);
        }

        public License Deserialize(byte[] canonicalJsonBytes)
        {
            return FromCanonicalBytes(canonicalJsonBytes);
        }

        /// <summary>
        /// Serializes a LicenseDocument to canonical UTF-8 bytes.
        /// Excludes "Signature" field and any fields in UnSerializedFields list.
        /// </summary>
        public byte[] SerializeLicenseDocument(LicenseDocument license)
        {
            ArgumentNullException.ThrowIfNull(license);
            
            // Convert LicenseDocument to JSON first
            var json = license.ToJson(validate: false); // Don't validate yet
            using var doc = JsonDocument.Parse(json);
            return BuildCanonicalUtf8BytesExcludingSignature(doc.RootElement, UnSerializedFields);
        }

        /// <summary>
        /// Deserializes a LicenseDocument from canonical JSON bytes.
        /// </summary>
        public LicenseDocument DeserializeLicenseDocument(byte[] canonicalJsonBytes)
        {
            ArgumentNullException.ThrowIfNull(canonicalJsonBytes);
            string json = Encoding.UTF8.GetString(canonicalJsonBytes);
            return LicenseDocument.FromJson(json);
        }

        /// <summary>
        /// Deserializes a LicenseDocument from canonical JSON string.
        /// </summary>
        public LicenseDocument DeserializeLicenseDocument(string json)
        {
            ArgumentNullException.ThrowIfNull(json);
            return LicenseDocument.FromJson(json);
        }

        /// <summary>
        /// Get canonical UTF-8 bytes from an object by serializing with given options,
        /// parsing the JSON, and then producing canonical JSON with sorted keys excluding "signature".
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private byte[] GetCanonicalUtf8BytesFromObject<T>(T obj, List<string>? unserializedFields = null)
        {
            // 1) Produce a deterministic initial JSON.
            var initialBytes = JsonSerializer.SerializeToUtf8Bytes(obj, Options);
            // 2) Parse produced JSON into JsonDocument and canonicalize (sort keys, compact).
            using var doc = JsonDocument.Parse(initialBytes);
            var root = doc.RootElement;
            return BuildCanonicalUtf8BytesExcludingSignature(root, unserializedFields);
        }

        /// <summary>
        /// Deserialize a License from canonical JSON bytes.
        /// </summary>
        /// <param name="canonicalJsonBytes"></param>
        /// <returns></returns>
        private License FromCanonicalBytes(byte[] canonicalJsonBytes)
        {
            ArgumentNullException.ThrowIfNull(canonicalJsonBytes);
            string json = Encoding.UTF8.GetString(canonicalJsonBytes);
            return JsonSerializer.Deserialize<License>(json, Options)
                ?? throw new InvalidOperationException("Deserialization produced null License.");
        }

        /// <summary>
        /// Build canonical UTF-8 bytes from a JsonElement root, excluding "signature" property (case-insensitive).
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private static byte[] BuildCanonicalUtf8BytesExcludingSignature(
            JsonElement root,
            IEnumerable<string>? excludedProperties = null)
        {
            using var ms = new MemoryStream();
            using (var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = false }))
            {
                if (root.ValueKind != JsonValueKind.Object) throw new ArgumentException("Root element must be a JSON object");

                writer.WriteStartObject();
                var exclude = new HashSet<string>(
                    excludedProperties ?? Array.Empty<string>(),
                    StringComparer.OrdinalIgnoreCase
                );
                exclude.Add("signature");
                var props = root.EnumerateObject()
                    .Where(p => !exclude.Contains(p.Name))
                    .OrderBy(p => p.Name, StringComparer.Ordinal)
                    .ToArray();

                foreach (var p in props)
                {
                    writer.WritePropertyName(p.Name);
                    WriteCanonicalElement(writer, p.Value);
                }
                writer.WriteEndObject();
                writer.Flush();
            }
            return ms.ToArray();
        }

        /// Recursively writes a <see cref="JsonElement"/> to a <see cref="Utf8JsonWriter"/> in canonical form.
        /// For objects, properties are written in ordinal sorted order by name, excluding any property named "signature" 
        /// (case-insensitive).
        /// For arrays, elements are written in their original order.
        /// Primitive values (strings, numbers, booleans, null) are written directly.
        /// </summary>
        /// <param name="writer">The <see cref="Utf8JsonWriter"/> to write the canonical JSON to.</param>
        /// <param name="elem">The <see cref="JsonElement"/> to canonicalize and write.</param>
        /// <remarks>
        /// This method ensures deterministic JSON output by:
        /// <list type="bullet">
        /// <item><description>Sorting object properties alphabetically by name using ordinal comparison</description></item>
        /// <item><description>Excluding "signature" properties at all levels of nesting</description></item>
        /// <item><description>Writing numbers in their appropriate numeric format (Int64, Decimal, or Double)</description></item>
        /// <item><description>Preserving array element order</description></item>
        /// </list>
        /// </remarks>
        private static void WriteCanonicalElement(
            Utf8JsonWriter writer,
            JsonElement elem)
        {
            switch (elem.ValueKind)
            {
                case JsonValueKind.Object:
                    writer.WriteStartObject();
                    foreach (var p in elem.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
                    {
                        //if (exclude.Contains(p.Name)) continue;
                        writer.WritePropertyName(p.Name);
                        WriteCanonicalElement(writer, p.Value);
                    }
                    writer.WriteEndObject();
                    break;

                case JsonValueKind.Array:
                    writer.WriteStartArray();
                    foreach (var item in elem.EnumerateArray()) WriteCanonicalElement(writer, item);
                    writer.WriteEndArray();
                    break;

                case JsonValueKind.String:
                    writer.WriteStringValue(elem.GetString());
                    break;

                case JsonValueKind.Number:
                    if (elem.TryGetInt64(out long l)) writer.WriteNumberValue(l);
                    else if (elem.TryGetDecimal(out decimal dec)) writer.WriteNumberValue(dec);
                    else if (elem.TryGetDouble(out double d)) writer.WriteNumberValue(d);
                    else writer.WriteRawValue(elem.GetRawText(), skipInputValidation: true);
                    break;

                case JsonValueKind.True:
                    writer.WriteBooleanValue(true);
                    break;

                case JsonValueKind.False:
                    writer.WriteBooleanValue(false);
                    break;

                case JsonValueKind.Null:
                    writer.WriteNullValue();
                    break;

                default:
                    writer.WriteRawValue(elem.GetRawText(), skipInputValidation: true);
                    break;
            }
        }

        static void MainCanonicalSerializer()
        {
            var original = new License(
                LicenseId: Guid.NewGuid().ToString(),
                LicenseInfo: "Standard Styx License",
                ExpiryUtc: null,
                MaxJunctions: null,
                AllowedFileHashes: null,
                Signature: null
            );
            CanonicalLicenseSerializer serializer = new CanonicalLicenseSerializer();
            LicenseSerializer standardSerializer = new LicenseSerializer();
            byte[] canonical = serializer.Serialize(original);
            License recovered = serializer.Deserialize(canonical);
            Console.WriteLine("Original License:");
            Console.WriteLine(standardSerializer.Serialize(original));
            Console.WriteLine("Recovered License from Canonical Bytes:");
            Console.WriteLine(standardSerializer.Serialize(recovered));
        }
    }
}