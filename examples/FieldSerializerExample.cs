using System;
using SimpleLicense.Core;

namespace SimpleLicense.Examples
{
    /// <summary>
    /// Demonstrates custom field serialization in License.ToJson().
    /// Field serializers convert field values to JSON-compatible formats.
    /// </summary>
    public class FieldSerializerExample
    {
        public static void Run()
        {
            ConsoleHelper.WriteHeader("Field Serializer Example");
            ConsoleHelper.WriteSeparator();
            
            // Example 1: Default ExpiryUtc Serializer
            ConsoleHelper.WriteSubHeader("1. Default ExpiryUtc Serializer");
            DemoDefaultExpiryUtcSerializer();
            
            // Example 2: Custom DateTime Serializer
            ConsoleHelper.WriteSubHeader("2. Custom DateTime Serializer");
            DemoCustomDateTimeSerializer();
            
            // Example 3: Custom Array Serializer
            ConsoleHelper.WriteSubHeader("3. Custom Array Serializer");
            DemoCustomArraySerializer();
            
            // Example 4: Custom Object Serializer
            ConsoleHelper.WriteSubHeader("4. Custom Object Serializer");
            DemoCustomObjectSerializer();
            
            ConsoleHelper.WriteDoubleSeparator();
        }
        
        private static void DemoDefaultExpiryUtcSerializer()
        {
            // The ExpiryUtc field has a default serializer that converts DateTime to ISO 8601 UTC
            var license = new License(ensureMandatoryKeys: true);
            license["LicenseId"] = "DEFAULT-001";
            license["ExpiryUtc"] = new DateTime(2027, 12, 31, 23, 59, 59, DateTimeKind.Utc);
            license["Signature"] = "default-sig";
            
            var json = license.ToJson(validate: false);
            
            Console.WriteLine("License with default ExpiryUtc serializer:");
            Console.WriteLine(json);
            Console.WriteLine();
        }
        
        private static void DemoCustomDateTimeSerializer()
        {
            // Register a custom serializer for a date field (simple date format)
            License.RegisterFieldSerializer("IssueDate", value =>
            {
                if (value is DateTime dt)
                {
                    return dt.ToString("yyyy-MM-dd"); // Simple date without time
                }
                return value;
            });
            
            var license = new License(ensureMandatoryKeys: false);
            license["LicenseId"] = "CUSTOM-DATE-001";
            license["IssueDate"] = new DateTime(2024, 1, 15, 10, 30, 0);
            
            var json = license.ToJson(validate: false);
            
            Console.WriteLine("License with custom IssueDate serializer (yyyy-MM-dd):");
            Console.WriteLine(json);
            Console.WriteLine();
        }
        
        private static void DemoCustomArraySerializer()
        {
            // Register a custom serializer for a features array (comma-separated string)
            License.RegisterFieldSerializer("Features", value =>
            {
                if (value is string[] features)
                {
                    return string.Join(", ", features);
                }
                return value;
            });
            
            var license = new License(ensureMandatoryKeys: false);
            license["LicenseId"] = "CUSTOM-ARRAY-001";
            license["Features"] = new[] { "Feature1", "Feature2", "Feature3" };
            
            var json = license.ToJson(validate: false);
            
            Console.WriteLine("License with custom Features serializer (comma-separated):");
            Console.WriteLine(json);
            Console.WriteLine();
        }
        
        private static void DemoCustomObjectSerializer()
        {
            // Register a custom serializer for user info (format as string)
            License.RegisterFieldSerializer("UserInfo", value =>
            {
                if (value is Dictionary<string, string> userInfo)
                {
                    return $"{userInfo.GetValueOrDefault("Name", "Unknown")} ({userInfo.GetValueOrDefault("Email", "no-email")})";
                }
                return value;
            });
            
            var license = new License(ensureMandatoryKeys: false);
            license["LicenseId"] = "CUSTOM-OBJ-001";
            license["UserInfo"] = new Dictionary<string, string>
            {
                { "Name", "John Doe" },
                { "Email", "john@example.com" }
            };
            
            var json = license.ToJson(validate: false);
            
            Console.WriteLine("License with custom UserInfo serializer (formatted string):");
            Console.WriteLine(json);
            Console.WriteLine();
        }
    }
}
