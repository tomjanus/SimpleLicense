using Xunit;
using Shouldly;
using SimpleLicense.LicenseValidation;

namespace SimpleLicense.Tests
{
    /// <summary>
    /// Tests for LicenseSchema - Schema creation, serialization, and validation
    /// </summary>
    public class LicenseSchemaTests : IDisposable
    {
        private readonly List<string> _tempFiles = new();

        public void Dispose()
        {
            // Clean up temporary files after each test
            foreach (var file in _tempFiles)
            {
                if (File.Exists(file))
                {
                    try { File.Delete(file); } catch { /* Ignore cleanup errors */ }
                }
            }
        }

        private string CreateTempFile(string extension)
        {
            var tempFile = Path.Combine(Path.GetTempPath(), $"test_schema_{Guid.NewGuid()}{extension}");
            _tempFiles.Add(tempFile);
            return tempFile;
        }

        #region Schema Creation Tests

        [Fact]
        public void CreateSchema_WithValidFields_ShouldSucceed()
        {
            // Arrange & Act
            var schema = new LicenseSchema(
                "TestSchema",
                new List<FieldDescriptor>
                {
                    new("Field1", "string", Required: true),
                    new("Field2", "int", Required: false)
                }
            );

            // Assert
            schema.ShouldNotBeNull();
            schema.Name.ShouldBe("TestSchema");
            schema.Fields.Count.ShouldBe(2);
            schema.Fields[0].Name.ShouldBe("Field1");
            schema.Fields[0].Type.ShouldBe("string");
            schema.Fields[0].Required.ShouldBeTrue();
        }

        [Fact]
        public void FieldDescriptor_WithDefaultValues_ShouldUseCorrectDefaults()
        {
            // Arrange & Act
            var field = new FieldDescriptor("TestField", "string");

            // Assert
            field.Name.ShouldBe("TestField");
            field.Type.ShouldBe("string");
            field.Signed.ShouldBeTrue(); // Default is true
            field.Required.ShouldBeFalse(); // Default is false
            field.DefaultValue.ShouldBeNull();
        }

        [Fact]
        public void FieldDescriptor_WithCustomValues_ShouldPreserveValues()
        {
            // Arrange & Act
            var field = new FieldDescriptor(
                "CustomField",
                "int",
                Signed: false,
                Required: true,
                DefaultValue: 42
            );

            // Assert
            field.Name.ShouldBe("CustomField");
            field.Type.ShouldBe("int");
            field.Signed.ShouldBeFalse();
            field.Required.ShouldBeTrue();
            field.DefaultValue.ShouldBe(42);
        }

        #endregion

        #region JSON Serialization Tests

        [Fact]
        public void FromJson_WithValidJson_ShouldDeserialize()
        {
            // Arrange
            var json = @"{
                ""Name"": ""JsonTestSchema"",
                ""Fields"": [
                    {
                        ""Name"": ""LicenseId"",
                        ""Type"": ""string"",
                        ""Signed"": true,
                        ""Required"": true
                    },
                    {
                        ""Name"": ""MaxUsers"",
                        ""Type"": ""int"",
                        ""Signed"": true,
                        ""Required"": false,
                        ""DefaultValue"": 10
                    }
                ]
            }";

            // Act
            var schema = LicenseSchema.FromJson(json);

            // Assert
            schema.ShouldNotBeNull();
            schema.Name.ShouldBe("JsonTestSchema");
            schema.Fields.Count.ShouldBe(2);
            schema.Fields[0].Name.ShouldBe("LicenseId");
            schema.Fields[0].Type.ShouldBe("string");
            schema.Fields[0].Required.ShouldBeTrue();
            schema.Fields[1].Name.ShouldBe("MaxUsers");
            schema.Fields[1].Type.ShouldBe("int");
        }

        [Fact]
        public void ToJson_WithValidSchema_ShouldSerialize()
        {
            // Arrange
            var schema = new LicenseSchema(
                "SerializationTest",
                new List<FieldDescriptor>
                {
                    new("Field1", "string", Required: true),
                    new("Field2", "bool", DefaultValue: true)
                }
            );

            // Act
            var json = LicenseSchema.ToJson(schema);

            // Assert
            json.ShouldNotBeNullOrWhiteSpace();
            json.ShouldContain("SerializationTest");
            json.ShouldContain("Field1");
            json.ShouldContain("Field2");
            json.ShouldContain("string");
            json.ShouldContain("bool");
        }

        [Fact]
        public void ToJson_ThenFromJson_ShouldRoundTrip()
        {
            // Arrange
            var originalSchema = new LicenseSchema(
                "RoundTripTest",
                new List<FieldDescriptor>
                {
                    new("LicenseId", "string", Signed: true, Required: true),
                    new("ExpiryUtc", "datetime", Signed: true, Required: true),
                    new("MaxUsers", "int", Signed: true, Required: false, DefaultValue: 100)
                }
            );

            // Act
            var json = LicenseSchema.ToJson(originalSchema);
            var deserializedSchema = LicenseSchema.FromJson(json);

            // Assert
            deserializedSchema.Name.ShouldBe(originalSchema.Name);
            deserializedSchema.Fields.Count.ShouldBe(originalSchema.Fields.Count);
            
            for (int i = 0; i < originalSchema.Fields.Count; i++)
            {
                deserializedSchema.Fields[i].Name.ShouldBe(originalSchema.Fields[i].Name);
                deserializedSchema.Fields[i].Type.ShouldBe(originalSchema.Fields[i].Type);
                deserializedSchema.Fields[i].Signed.ShouldBe(originalSchema.Fields[i].Signed);
                deserializedSchema.Fields[i].Required.ShouldBe(originalSchema.Fields[i].Required);
            }
        }

        [Fact]
        public void FromJson_WithInvalidJson_ShouldThrow()
        {
            // Arrange
            var invalidJson = "{ this is not valid json }";

            // Act & Assert
            Should.Throw<Exception>(() => LicenseSchema.FromJson(invalidJson));
        }

        [Fact]
        public void ToJson_WithNullSchema_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Should.Throw<ArgumentNullException>(() => LicenseSchema.ToJson(null!));
        }

        #endregion

        #region YAML Serialization Tests

        [Fact]
        public void FromYaml_WithValidYaml_ShouldDeserialize()
        {
            // Arrange
            var yaml = @"
name: YamlTestSchema
fields:
  - name: LicenseId
    type: string
    signed: true
    required: true
  - name: MaxUsers
    type: int
    signed: true
    required: false
    defaultValue: 10
";

            // Act
            var schema = LicenseSchema.FromYaml(yaml);

            // Assert
            schema.ShouldNotBeNull();
            schema.Name.ShouldBe("YamlTestSchema");
            schema.Fields.Count.ShouldBe(2);
            schema.Fields[0].Name.ShouldBe("LicenseId");
            schema.Fields[0].Type.ShouldBe("string");
            schema.Fields[0].Required.ShouldBeTrue();
            schema.Fields[1].Name.ShouldBe("MaxUsers");
            schema.Fields[1].Type.ShouldBe("int");
        }

        [Fact]
        public void ToYaml_WithValidSchema_ShouldSerialize()
        {
            // Arrange
            var schema = new LicenseSchema(
                "YamlSerializationTest",
                new List<FieldDescriptor>
                {
                    new("Field1", "string", Required: true),
                    new("Field2", "bool", DefaultValue: true)
                }
            );

            // Act
            var yaml = LicenseSchema.ToYaml(schema);

            // Assert
            yaml.ShouldNotBeNullOrWhiteSpace();
            yaml.ShouldContain("YamlSerializationTest");
            yaml.ShouldContain("Field1");
            yaml.ShouldContain("Field2");
            yaml.ShouldContain("string");
            yaml.ShouldContain("bool");
        }

        [Fact]
        public void ToYaml_ThenFromYaml_ShouldRoundTrip()
        {
            // Arrange
            var originalSchema = new LicenseSchema(
                "YamlRoundTripTest",
                new List<FieldDescriptor>
                {
                    new("LicenseId", "string", Signed: true, Required: true),
                    new("ExpiryUtc", "datetime", Signed: true, Required: true),
                    new("IsActive", "bool", Signed: true, Required: false, DefaultValue: true)
                }
            );

            // Act
            var yaml = LicenseSchema.ToYaml(originalSchema);
            var deserializedSchema = LicenseSchema.FromYaml(yaml);

            // Assert
            deserializedSchema.Name.ShouldBe(originalSchema.Name);
            deserializedSchema.Fields.Count.ShouldBe(originalSchema.Fields.Count);
            
            for (int i = 0; i < originalSchema.Fields.Count; i++)
            {
                deserializedSchema.Fields[i].Name.ShouldBe(originalSchema.Fields[i].Name);
                deserializedSchema.Fields[i].Type.ShouldBe(originalSchema.Fields[i].Type);
                deserializedSchema.Fields[i].Signed.ShouldBe(originalSchema.Fields[i].Signed);
                deserializedSchema.Fields[i].Required.ShouldBe(originalSchema.Fields[i].Required);
            }
        }

        [Fact]
        public void ToYaml_WithNullSchema_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Should.Throw<ArgumentNullException>(() => LicenseSchema.ToYaml(null!));
        }

        #endregion

        #region File Operations Tests

        [Fact]
        public void FromFile_WithJsonFile_ShouldLoadSchema()
        {
            // Arrange
            var tempFile = CreateTempFile(".json");
            var schema = new LicenseSchema(
                "FileTestSchema",
                new List<FieldDescriptor>
                {
                    new("TestField", "string", Required: true)
                }
            );
            var json = LicenseSchema.ToJson(schema);
            File.WriteAllText(tempFile, json);

            // Act
            var loadedSchema = LicenseSchema.FromFile(tempFile);

            // Assert
            loadedSchema.Name.ShouldBe("FileTestSchema");
            loadedSchema.Fields.Count.ShouldBe(1);
            loadedSchema.Fields[0].Name.ShouldBe("TestField");
        }

        [Fact]
        public void FromFile_WithYamlFile_ShouldLoadSchema()
        {
            // Arrange
            var tempFile = CreateTempFile(".yaml");
            var schema = new LicenseSchema(
                "YamlFileTestSchema",
                new List<FieldDescriptor>
                {
                    new("YamlField", "int", Required: false)
                }
            );
            var yaml = LicenseSchema.ToYaml(schema);
            File.WriteAllText(tempFile, yaml);

            // Act
            var loadedSchema = LicenseSchema.FromFile(tempFile);

            // Assert
            loadedSchema.Name.ShouldBe("YamlFileTestSchema");
            loadedSchema.Fields.Count.ShouldBe(1);
            loadedSchema.Fields[0].Name.ShouldBe("YamlField");
        }

        [Fact]
        public void FromFile_WithYmlExtension_ShouldLoadSchema()
        {
            // Arrange
            var tempFile = CreateTempFile(".yml");
            var yaml = @"
name: YmlTestSchema
fields:
  - name: TestField
    type: string
    required: true
";
            File.WriteAllText(tempFile, yaml);

            // Act
            var loadedSchema = LicenseSchema.FromFile(tempFile);

            // Assert
            loadedSchema.Name.ShouldBe("YmlTestSchema");
            loadedSchema.Fields.Count.ShouldBe(1);
        }

        [Fact]
        public void FromFile_WithNonExistentFile_ShouldThrowFileNotFoundException()
        {
            // Arrange
            var nonExistentFile = Path.Combine(Path.GetTempPath(), "non_existent_schema.json");

            // Act & Assert
            Should.Throw<FileNotFoundException>(() => LicenseSchema.FromFile(nonExistentFile));
        }

        [Fact]
        public void ToFile_WithJsonExtension_ShouldSaveAsJson()
        {
            // Arrange
            var tempFile = CreateTempFile(".json");
            var schema = new LicenseSchema(
                "SaveTestSchema",
                new List<FieldDescriptor>
                {
                    new("SavedField", "string", Required: true)
                }
            );

            // Act
            LicenseSchema.ToFile(schema, tempFile);

            // Assert
            File.Exists(tempFile).ShouldBeTrue();
            var content = File.ReadAllText(tempFile);
            content.ShouldContain("SaveTestSchema");
            content.ShouldContain("SavedField");
            
            // Verify it's valid JSON by loading it back
            var loadedSchema = LicenseSchema.FromFile(tempFile);
            loadedSchema.Name.ShouldBe("SaveTestSchema");
        }

        [Fact]
        public void ToFile_WithYamlExtension_ShouldSaveAsYaml()
        {
            // Arrange
            var tempFile = CreateTempFile(".yaml");
            var schema = new LicenseSchema(
                "YamlSaveTest",
                new List<FieldDescriptor>
                {
                    new("YamlSavedField", "int", Required: false)
                }
            );

            // Act
            LicenseSchema.ToFile(schema, tempFile);

            // Assert
            File.Exists(tempFile).ShouldBeTrue();
            var content = File.ReadAllText(tempFile);
            content.ShouldContain("YamlSaveTest");
            content.ShouldContain("YamlSavedField");
            
            // Verify it's valid YAML by loading it back
            var loadedSchema = LicenseSchema.FromFile(tempFile);
            loadedSchema.Name.ShouldBe("YamlSaveTest");
        }

        [Fact]
        public void ToFile_WithUnsupportedExtension_ShouldThrow()
        {
            // Arrange
            var tempFile = CreateTempFile(".txt");
            var schema = new LicenseSchema("Test", new List<FieldDescriptor> { new("F", "string") });

            // Act & Assert
            Should.Throw<InvalidOperationException>(() => LicenseSchema.ToFile(schema, tempFile));
        }

        [Fact]
        public void ToFile_WithNullSchema_ShouldThrowArgumentNullException()
        {
            // Arrange
            var tempFile = CreateTempFile(".json");

            // Act & Assert
            Should.Throw<ArgumentNullException>(() => LicenseSchema.ToFile(null!, tempFile));
        }

        [Fact]
        public void FromFile_AutoDetectsJson_WhenNoExtension()
        {
            // Arrange
            var tempFile = CreateTempFile("");
            var json = @"{
                ""Name"": ""AutoDetectJson"",
                ""Fields"": [{""Name"": ""F1"", ""Type"": ""string""}]
            }";
            File.WriteAllText(tempFile, json);

            // Act
            var schema = LicenseSchema.FromFile(tempFile);

            // Assert
            schema.Name.ShouldBe("AutoDetectJson");
        }

        #endregion

        #region Validation Tests

        [Fact]
        public void Validate_WithValidSchema_ShouldNotThrow()
        {
            // Arrange
            var schema = new LicenseSchema(
                "ValidSchema",
                new List<FieldDescriptor>
                {
                    new("Field1", "string", Required: true),
                    new("Field2", "int", Required: false)
                }
            );

            // Act & Assert
            Should.NotThrow(() => schema.Validate());
        }

        [Fact]
        public void TryValidate_WithValidSchema_ShouldReturnTrue()
        {
            // Arrange
            var schema = new LicenseSchema(
                "ValidSchema",
                new List<FieldDescriptor>
                {
                    new("Field1", "string", Required: true)
                }
            );

            // Act
            var isValid = schema.TryValidate(out var errors);

            // Assert
            isValid.ShouldBeTrue();
            errors.ShouldBeEmpty();
        }

        [Fact]
        public void Validate_WithEmptyName_ShouldThrow()
        {
            // Arrange
            var schema = new LicenseSchema(
                "",
                new List<FieldDescriptor>
                {
                    new("Field1", "string")
                }
            );

            // Act & Assert
            var exception = Should.Throw<InvalidOperationException>(() => schema.Validate());
            exception.Message.ShouldContain("Schema name must not be empty");
        }

        [Fact]
        public void TryValidate_WithEmptyName_ShouldReturnFalse()
        {
            // Arrange
            var schema = new LicenseSchema(
                "",
                new List<FieldDescriptor>
                {
                    new("Field1", "string")
                }
            );

            // Act
            var isValid = schema.TryValidate(out var errors);

            // Assert
            isValid.ShouldBeFalse();
            errors.ShouldNotBeEmpty();
            errors.ShouldContain(e => e.Contains("Schema name"));
        }

        [Fact]
        public void Validate_WithNoFields_ShouldThrow()
        {
            // Arrange
            var schema = new LicenseSchema("TestSchema", new List<FieldDescriptor>());

            // Act & Assert
            var exception = Should.Throw<InvalidOperationException>(() => schema.Validate());
            exception.Message.ShouldContain("at least one field");
        }

        [Fact]
        public void Validate_WithDuplicateFieldNames_ShouldThrow()
        {
            // Arrange
            var schema = new LicenseSchema(
                "DuplicateTest",
                new List<FieldDescriptor>
                {
                    new("DuplicateField", "string"),
                    new("DuplicateField", "int")
                }
            );

            // Act & Assert
            var exception = Should.Throw<InvalidOperationException>(() => schema.Validate());
            exception.Message.ShouldContain("unique");
            exception.Message.ShouldContain("DuplicateField");
        }

        [Fact]
        public void Validate_WithUnsupportedType_ShouldThrow()
        {
            // Arrange
            var schema = new LicenseSchema(
                "UnsupportedTypeTest",
                new List<FieldDescriptor>
                {
                    new("Field1", "unsupported_type")
                }
            );

            // Act & Assert
            var exception = Should.Throw<InvalidOperationException>(() => schema.Validate());
            exception.Message.ShouldContain("Unsupported type");
            exception.Message.ShouldContain("unsupported_type");
        }

        [Fact]
        public void Validate_WithIncompatibleDefaultValue_ShouldThrow()
        {
            // Arrange
            var schema = new LicenseSchema(
                "IncompatibleDefaultTest",
                new List<FieldDescriptor>
                {
                    new("IntField", "int", DefaultValue: "not_a_number")
                }
            );

            // Act & Assert
            var exception = Should.Throw<InvalidOperationException>(() => schema.Validate());
            exception.Message.ShouldContain("Default value");
            exception.Message.ShouldContain("incompatible");
        }

        [Fact]
        public void Validate_WithEmptyFieldName_ShouldThrow()
        {
            // Arrange
            var schema = new LicenseSchema(
                "EmptyFieldNameTest",
                new List<FieldDescriptor>
                {
                    new("", "string")
                }
            );

            // Act & Assert
            var exception = Should.Throw<InvalidOperationException>(() => schema.Validate());
            exception.Message.ShouldContain("non-empty Name");
        }

        [Fact]
        public void Validate_WithAllSupportedTypes_ShouldNotThrow()
        {
            // Arrange
            var schema = new LicenseSchema(
                "AllTypesTest",
                new List<FieldDescriptor>
                {
                    new("StringField", "string"),
                    new("IntField", "int"),
                    new("DoubleField", "double"),
                    new("DecimalField", "decimal"),
                    new("BoolField", "bool"),
                    new("DateTimeField", "datetime"),
                    new("ListStringField", "list<string>"),
                    new("ListIntField", "list<int>"),
                    new("ListDoubleField", "list<double>"),
                    new("ListBoolField", "list<bool>")
                }
            );

            // Act & Assert
            Should.NotThrow(() => schema.Validate());
        }

        [Fact]
        public void TryValidate_WithMultipleErrors_ShouldReturnAllErrors()
        {
            // Arrange
            var schema = new LicenseSchema(
                "", // Empty name
                new List<FieldDescriptor>
                {
                    new("", "string"), // Empty field name
                    new("Field2", "invalid_type"), // Invalid type
                    new("Field3", "int", DefaultValue: "not_an_int") // Incompatible default
                }
            );

            // Act
            var isValid = schema.TryValidate(out var errors);

            // Assert
            isValid.ShouldBeFalse();
            errors.Count.ShouldBeGreaterThan(2);
        }

        #endregion

        #region Complex Scenarios

        [Fact]
        public void ComplexSchema_WithAllFeatures_ShouldRoundTripCorrectly()
        {
            // Arrange
            var originalSchema = new LicenseSchema(
                "ComplexSchema",
                new List<FieldDescriptor>
                {
                    new("LicenseId", "string", Signed: true, Required: true),
                    new("ExpiryUtc", "datetime", Signed: true, Required: true),
                    new("Signature", "string", Signed: false, Required: true),
                    new("CustomerName", "string", Signed: true, Required: true),
                    new("MaxUsers", "int", Signed: true, Required: false, DefaultValue: 10),
                    new("Features", "list<string>", Signed: true, Required: false),
                    new("IsActive", "bool", Signed: true, Required: false, DefaultValue: true),
                    new("Price", "double", Signed: true, Required: false, DefaultValue: 99.99)
                }
            );

            // Act - Round trip through JSON
            var json = LicenseSchema.ToJson(originalSchema);
            var jsonSchema = LicenseSchema.FromJson(json);

            // Act - Round trip through YAML
            var yaml = LicenseSchema.ToYaml(originalSchema);
            var yamlSchema = LicenseSchema.FromYaml(yaml);

            // Act - Round trip through file
            var tempJsonFile = CreateTempFile(".json");
            LicenseSchema.ToFile(originalSchema, tempJsonFile);
            var fileSchema = LicenseSchema.FromFile(tempJsonFile);

            // Assert - All should match original
            foreach (var schema in new[] { jsonSchema, yamlSchema, fileSchema })
            {
                schema.Name.ShouldBe(originalSchema.Name);
                schema.Fields.Count.ShouldBe(originalSchema.Fields.Count);
                
                for (int i = 0; i < originalSchema.Fields.Count; i++)
                {
                    schema.Fields[i].Name.ShouldBe(originalSchema.Fields[i].Name);
                    schema.Fields[i].Type.ShouldBe(originalSchema.Fields[i].Type);
                    schema.Fields[i].Signed.ShouldBe(originalSchema.Fields[i].Signed);
                    schema.Fields[i].Required.ShouldBe(originalSchema.Fields[i].Required);
                }
            }
        }

        #endregion
    }
}
