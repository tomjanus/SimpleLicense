using SimpleLicense.Core;
using SimpleLicense.Core.LicenseValidation;
using Xunit;

namespace SimpleLicense.Tests
{
    public class FieldProcessorsTests
    {
        // ═══════════════════════════════════════════════════════
        // BUILT-IN PROCESSOR TESTS
        // ═══════════════════════════════════════════════════════

        [Fact]
        public void HashFilesProcessor_ComputesHashesForMultipleFiles()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            try
            {
                var file1 = Path.Combine(tempDir, "test1.txt");
                var file2 = Path.Combine(tempDir, "test2.txt");
                File.WriteAllText(file1, "Content 1");
                File.WriteAllText(file2, "Content 2");

                var processor = FieldProcessors.Get("HashFiles");
                Assert.NotNull(processor);

                var context = new ProcessorContext
                {
                    FieldName = "Files",
                    WorkingDirectory = tempDir
                };

                // Act
                var result = processor(new[] { "test1.txt", "test2.txt" }, context);

                // Assert
                Assert.NotNull(result);
                var hashes = Assert.IsType<Dictionary<string, string>>(result);
                Assert.Equal(2, hashes.Count);
                Assert.Contains("test1.txt", hashes.Keys);
                Assert.Contains("test2.txt", hashes.Keys);
                Assert.NotEmpty(hashes["test1.txt"]);
                Assert.NotEmpty(hashes["test2.txt"]);
                Assert.NotEqual(hashes["test1.txt"], hashes["test2.txt"]);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void HashFileProcessor_ComputesHashForSingleFile()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "Test content");
            
            try
            {
                var processor = FieldProcessors.Get("HashFile");
                Assert.NotNull(processor);

                var context = new ProcessorContext
                {
                    FieldName = "File",
                    WorkingDirectory = Path.GetDirectoryName(tempFile)
                };

                // Act
                var result = processor(tempFile, context);

                // Assert
                Assert.NotNull(result);
                var hash = Assert.IsType<string>(result);
                Assert.Equal(64, hash.Length); // SHA256 hex string length
                Assert.Matches("^[a-f0-9]{64}$", hash);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void HashFilesProcessor_ThrowsExceptionForNonExistentFile()
        {
            // Arrange
            var processor = FieldProcessors.Get("HashFiles");
            Assert.NotNull(processor);

            var context = new ProcessorContext
            {
                FieldName = "Files",
                WorkingDirectory = Path.GetTempPath()
            };

            // Act & Assert
            var ex = Assert.Throws<FileNotFoundException>(() =>
                processor(new[] { "nonexistent_file.txt" }, context));
            Assert.Contains("nonexistent_file.txt", ex.Message);
        }

        [Fact]
        public void GenerateGuidProcessor_GeneratesValidGuid()
        {
            // Arrange
            var processor = FieldProcessors.Get("GenerateGuid");
            Assert.NotNull(processor);

            var context = new ProcessorContext { FieldName = "Id" };

            // Act
            var result1 = processor(null, context);
            var result2 = processor(null, context);

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            var guid1 = Guid.Parse(result1.ToString()!);
            var guid2 = Guid.Parse(result2.ToString()!);
            Assert.NotEqual(guid1, guid2); // Each call generates unique GUID
        }

        [Fact]
        public void CurrentTimestampProcessor_ReturnsUtcDateTime()
        {
            // Arrange
            var processor = FieldProcessors.Get("CurrentTimestamp");
            Assert.NotNull(processor);

            var context = new ProcessorContext { FieldName = "CreatedAt" };
            var before = DateTime.UtcNow;

            // Act
            var result = processor(null, context);

            // Assert
            var after = DateTime.UtcNow;
            Assert.NotNull(result);
            var timestamp = Assert.IsType<DateTime>(result);
            Assert.True(timestamp >= before && timestamp <= after);
            Assert.Equal(DateTimeKind.Utc, timestamp.Kind);
        }

        [Fact]
        public void ToUpperProcessor_ConvertsStringToUppercase()
        {
            // Arrange
            var processor = FieldProcessors.Get("ToUpper");
            Assert.NotNull(processor);

            var context = new ProcessorContext { FieldName = "Name" };

            // Act
            var result = processor("hello world", context);

            // Assert
            Assert.Equal("HELLO WORLD", result);
        }

        [Fact]
        public void ToLowerProcessor_ConvertsStringToLowercase()
        {
            // Arrange
            var processor = FieldProcessors.Get("ToLower");
            Assert.NotNull(processor);

            var context = new ProcessorContext { FieldName = "Name" };

            // Act
            var result = processor("HELLO WORLD", context);

            // Assert
            Assert.Equal("hello world", result);
        }

        [Fact]
        public void PassThroughProcessor_ReturnsValueUnchanged()
        {
            // Arrange
            var processor = FieldProcessors.Get("PassThrough");
            Assert.NotNull(processor);

            var context = new ProcessorContext { FieldName = "Value" };
            var inputValue = new { Name = "Test", Count = 42 };

            // Act
            var result = processor(inputValue, context);

            // Assert
            Assert.Same(inputValue, result);
        }

        // ═══════════════════════════════════════════════════════
        // PROCESSOR REGISTRY TESTS
        // ═══════════════════════════════════════════════════════

        [Fact]
        public void Register_AddsCustomProcessor()
        {
            // Arrange
            var processorName = $"TestProcessor_{Guid.NewGuid()}";
            FieldProcessor customProcessor = (value, ctx) => $"Processed: {value}";

            // Act
            FieldProcessors.Register(processorName, customProcessor);

            // Assert
            Assert.True(FieldProcessors.IsRegistered(processorName));
            var retrieved = FieldProcessors.Get(processorName);
            Assert.NotNull(retrieved);
            
            var result = retrieved("test", new ProcessorContext());
            Assert.Equal("Processed: test", result);
        }

        [Fact]
        public void Unregister_RemovesProcessor()
        {
            // Arrange
            var processorName = $"TempProcessor_{Guid.NewGuid()}";
            FieldProcessors.Register(processorName, (v, c) => v);

            // Act
            var removed = FieldProcessors.Unregister(processorName);

            // Assert
            Assert.True(removed);
            Assert.False(FieldProcessors.IsRegistered(processorName));
            Assert.Null(FieldProcessors.Get(processorName));
        }

        [Fact]
        public void Get_ReturnsNullForUnregisteredProcessor()
        {
            // Act
            var processor = FieldProcessors.Get("NonExistentProcessor_12345");

            // Assert
            Assert.Null(processor);
        }

        [Fact]
        public void All_ReturnsAllRegisteredProcessors()
        {
            // Act
            var allProcessors = FieldProcessors.All;

            // Assert
            Assert.NotNull(allProcessors);
            Assert.NotEmpty(allProcessors);
            Assert.Contains("HashFiles", allProcessors.Keys);
            Assert.Contains("GenerateGuid", allProcessors.Keys);
            Assert.Contains("CurrentTimestamp", allProcessors.Keys);
        }

        // ═══════════════════════════════════════════════════════
        // INTEGRATION TESTS WITH LICENSE CREATOR
        // ═══════════════════════════════════════════════════════

        [Fact]
        public void CreateLicense_AppliesProcessors()
        {
            // Arrange
            var schema = new LicenseSchema(
                "TestSchema",
                new List<FieldDescriptor>
                {
                    new("LicenseId", "string", Required: true, Processor: "GenerateGuid"),
                    new("CreatedUtc", "datetime", Required: true, Processor: "CurrentTimestamp"),
                    new("CustomerName", "string", Required: true, Processor: "ToUpper"),
                    new("Signature", "string", Required: false)
                }
            );

            var input = new Dictionary<string, object?>
            {
                ["CustomerName"] = "acme corp"
            };

            var creator = new LicenseCreator();

            // Act
            var license = creator.CreateLicense(schema, input);

            // Assert
            Assert.NotNull(license["LicenseId"]);
            Assert.IsType<string>(license["LicenseId"]);
            
            Assert.NotNull(license["CreatedUtc"]);
            Assert.IsType<DateTime>(license["CreatedUtc"]);
            
            Assert.Equal("ACME CORP", license["CustomerName"]);
        }

        [Fact]
        public void CreateLicense_WithFileHashing()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            try
            {
                var file1 = Path.Combine(tempDir, "app.exe");
                var file2 = Path.Combine(tempDir, "data.dll");
                File.WriteAllText(file1, "Application binary");
                File.WriteAllText(file2, "Data library");

                var schema = new LicenseSchema(
                    "FileProtectionLicense",
                    new List<FieldDescriptor>
                    {
                        new("LicenseId", "string", Required: true),
                        new("ProtectedFiles", "list<string>", Required: true, Processor: "HashFiles"),
                        new("Signature", "string", Required: false)
                    }
                );

                var input = new Dictionary<string, object?>
                {
                    ["LicenseId"] = "TEST-123",
                    ["ProtectedFiles"] = new List<string> { "app.exe", "data.dll" }
                };

                var creator = new LicenseCreator();

                // Act
                var license = creator.CreateLicense(schema, input, workingDirectory: tempDir);

                // Assert
                Assert.NotNull(license["ProtectedFiles"]);
                var hashes = Assert.IsType<Dictionary<string, string>>(license["ProtectedFiles"]);
                Assert.Equal(2, hashes.Count);
                Assert.Contains("app.exe", hashes.Keys);
                Assert.Contains("data.dll", hashes.Keys);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void CreateLicense_ThrowsWhenProcessorNotFound()
        {
            // Arrange
            var schema = new LicenseSchema(
                "TestSchema",
                new List<FieldDescriptor>
                {
                    new("TestField", "string", Processor: "NonExistentProcessor")
                }
            );

            var input = new Dictionary<string, object?> { ["TestField"] = "value" };
            var creator = new LicenseCreator();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                creator.CreateLicense(schema, input));
            Assert.Contains("NonExistentProcessor", ex.Message);
            Assert.Contains("not registered", ex.Message);
        }

        [Fact]
        public void CreateLicense_AppliesDefaultValues()
        {
            // Arrange
            var schema = new LicenseSchema(
                "TestSchema",
                new List<FieldDescriptor>
                {
                    new("LicenseId", "string", Required: true),
                    new("DefaultField", "string", DefaultValue: "default_value", Processor: "ToUpper"),
                    new("Signature", "string", Required: false)
                }
            );

            var input = new Dictionary<string, object?>
            {
                ["LicenseId"] = "TEST-123"
                // DefaultField not provided, should use default value
            };

            var creator = new LicenseCreator();

            // Act
            var license = creator.CreateLicense(schema, input);

            // Assert
            Assert.Equal("DEFAULT_VALUE", license["DefaultField"]); // Default applied and processed
        }

        [Fact]
        public void CreateLicense_ProcessorContextReceivesParameters()
        {
            // Arrange
            var processorName = $"ParamTestProcessor_{Guid.NewGuid()}";
            string? capturedParam = null;
            
            FieldProcessors.Register(processorName, (value, context) =>
            {
                capturedParam = context.Parameters.TryGetValue("testParam", out var p) 
                    ? p.ToString() 
                    : null;
                return value;
            });

            var schema = new LicenseSchema(
                "TestSchema",
                new List<FieldDescriptor>
                {
                    new("TestField", "string", Processor: processorName)
                }
            );

            var input = new Dictionary<string, object?> { ["TestField"] = "value" };
            var parameters = new Dictionary<string, object> { ["testParam"] = "test_value" };
            var creator = new LicenseCreator();

            // Act
            creator.CreateLicense(schema, input, processorParameters: parameters);

            // Assert
            Assert.Equal("test_value", capturedParam);
        }

        [Fact]
        public void CreateLicenseFromFile_LoadsAndProcesses()
        {
            // Arrange
            var tempSchemaPath = Path.Combine(Path.GetTempPath(), $"test_schema_{Guid.NewGuid()}.yaml");
            
            try
            {
                var schema = new LicenseSchema(
                    "FileTestSchema",
                    new List<FieldDescriptor>
                    {
                        new("LicenseId", "string", Required: true, Processor: "GenerateGuid"),
                        new("Name", "string", Required: true, Processor: "ToLower")
                    }
                );

                LicenseSchema.ToFile(schema, tempSchemaPath);

                var input = new Dictionary<string, object?>
                {
                    ["Name"] = "TEST NAME"
                };

                var creator = new LicenseCreator();

                // Act
                var license = creator.CreateLicenseFromFile(tempSchemaPath, input);

                // Assert
                Assert.NotNull(license["LicenseId"]);
                Assert.Equal("test name", license["Name"]);
            }
            finally
            {
                if (File.Exists(tempSchemaPath))
                    File.Delete(tempSchemaPath);
            }
        }
    }
}
