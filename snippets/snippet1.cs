    
/// Serializing and deserializing schemas and licenses in json format to and from json files.
namespace SimpleLicense.Core
{
    static class Program
    {
        static void Main(string[] args)
        {
            var json_txt = File.ReadAllText("sample_schema.json"); // or embed
            LicenseSchema schema =  LicenseSchema.FromJson(json_txt);
            string schema_json = LicenseSchema.ToJson(schema);
            Console.WriteLine($"Loaded schema: {schema_json}   ");
            Console.WriteLine("Done.  ");
            Console.WriteLine("Loading license.....");
            var license = LicenseDocument.FromJson(File.ReadAllText("sample_license.json"));
            license.EnsureMandatoryPresent();
            Console.WriteLine("License loaded successfully.");
            var license_json = license.ToJson();
            Console.WriteLine($"License JSON: {license_json}");
        }
    }
}

/// Registering canonicalizers 
/// Canonicalizers are used to put files, e.g. input files for the software that will be hashed
/// into a 'tidy' form without breaks, excessive spacing, etc. such that characters that do not
/// contain any information do not affect file hashes.
namespace SimpleLicense.Core
{
    static class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Loading canonicalizer registry...");
            CanonicalizerRegistry.LoadFromJson("../../config/canonicalizers.json");
            Console.WriteLine("Canonicalizer registry loaded.");
            foreach (var (ext, can) in CanonicalizerRegistry.Registry)
            {
                Console.WriteLine($"Extension: {ext}, Canonicalizer: {can.GetType().FullName}");
            }
        }
    }
}
