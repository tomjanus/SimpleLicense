using Xunit;
using Shouldly;
using SimpleLicense.Core.Canonicalizers;

namespace SimpleLicense.Tests
{
    /// <summary>
    /// Tests for GenericTextFileCanonicalizer - Generic text file canonicalization
    /// </summary>
    public class GenericTextFileCanonalizerTests
    {
        private readonly GenericTextFileCanonicalizer _canonicalizer;

        public GenericTextFileCanonalizerTests()
        {
            _canonicalizer = new GenericTextFileCanonicalizer();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNoArguments_ShouldUseDefaultExtensions()
        {
            // Act
            var canonicalizer = new GenericTextFileCanonicalizer();

            // Assert
            var extensions = canonicalizer.SupportedExtensions.ToList();
            extensions.ShouldContain(".txt");
            extensions.ShouldContain(".md");
            extensions.ShouldContain(".csv");
            extensions.ShouldContain(".log");
            extensions.ShouldContain(".ini");
            extensions.ShouldContain(".conf");
        }

        [Fact]
        public void Constructor_WithCustomExtensions_ShouldUseProvidedExtensions()
        {
            // Arrange
            var customExtensions = new[] { ".custom", ".test" };

            // Act
            var canonicalizer = new GenericTextFileCanonicalizer(customExtensions);

            // Assert
            var extensions = canonicalizer.SupportedExtensions.ToList();
            extensions.ShouldContain(".custom");
            extensions.ShouldContain(".test");
            extensions.ShouldNotContain(".txt");
        }

        [Fact]
        public void Constructor_WithNullExtensions_ShouldUseDefaultExtensions()
        {
            // Act
            var canonicalizer = new GenericTextFileCanonicalizer(null);

            // Assert
            var extensions = canonicalizer.SupportedExtensions.ToList();
            extensions.ShouldContain(".txt");
            extensions.ShouldContain(".md");
        }

        [Fact]
        public void Constructor_WithEmptyExtensions_ShouldUseDefaultExtensions()
        {
            // Act
            var canonicalizer = new GenericTextFileCanonicalizer(Array.Empty<string>());

            // Assert
            var extensions = canonicalizer.SupportedExtensions.ToList();
            extensions.ShouldContain(".txt");
            extensions.ShouldContain(".md");
        }

        [Fact]
        public void SupportedExtensions_ShouldBeCaseInsensitive()
        {
            // Arrange
            var canonicalizer = new GenericTextFileCanonicalizer(new[] { ".TXT", ".Md" });

            // Act
            var extensions = canonicalizer.SupportedExtensions;

            // Assert
            extensions.ShouldContain(".txt");
            extensions.ShouldContain(".md");
        }

        #endregion

        #region Line Ending Normalization Tests

        [Fact]
        public void Canonicalize_WithWindowsLineEndings_ShouldNormalizeToUnix()
        {
            // Arrange
            var input = "line1\r\nline2\r\nline3";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldBe("line1\nline2\nline3\n");
        }

        [Fact]
        public void Canonicalize_WithMacLineEndings_ShouldNormalizeToUnix()
        {
            // Arrange
            var input = "line1\rline2\rline3";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldBe("line1\nline2\nline3\n");
        }

        [Fact]
        public void Canonicalize_WithMixedLineEndings_ShouldNormalizeToUnix()
        {
            // Arrange
            var input = "line1\r\nline2\nline3\rline4";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldBe("line1\nline2\nline3\nline4\n");
        }

        [Fact]
        public void Canonicalize_WithUnixLineEndings_ShouldRemainUnchanged()
        {
            // Arrange
            var input = "line1\nline2\nline3";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldBe("line1\nline2\nline3\n");
        }

        #endregion

        #region Whitespace Handling Tests

        [Fact]
        public void Canonicalize_WithTrailingWhitespace_ShouldRemove()
        {
            // Arrange
            var input = "line1   \nline2\t\t\nline3  ";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldBe("line1\nline2\nline3\n");
        }

        [Fact]
        public void Canonicalize_WithLeadingWhitespace_ShouldPreserve()
        {
            // Arrange
            var input = "    line1\n\t\tline2\n  line3";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            // Tabs are converted to spaces (count preserved)
            result.ShouldBe("    line1\n  line2\n  line3\n");
        }

        [Fact]
        public void Canonicalize_WithInternalWhitespaceRuns_ShouldCollapse()
        {
            // Arrange
            var input = "word1    word2\nword3\t\t\tword4";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldBe("word1 word2\nword3 word4\n");
        }

        [Fact]
        public void Canonicalize_WithLeadingAndInternalWhitespace_ShouldPreserveLeading()
        {
            // Arrange
            var input = "    word1    word2\n\tword3\t\tword4";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            // Tabs are converted to spaces (count preserved)
            result.ShouldBe("    word1 word2\n word3 word4\n");
        }

        [Fact]
        public void Canonicalize_WithEmptyLines_ShouldRemove()
        {
            // Arrange
            var input = "line1\n\nline2\n   \nline3";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldBe("line1\nline2\nline3\n");
        }

        [Fact]
        public void Canonicalize_WithOnlyWhitespaceLines_ShouldRemove()
        {
            // Arrange
            var input = "line1\n     \nline2\n\t\t\t\nline3";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldBe("line1\nline2\nline3\n");
        }

        #endregion

        #region Comment Handling Tests

        [Fact]
        public void Canonicalize_WithHashComments_ShouldRemove()
        {
            // Arrange
            var input = "# This is a comment\nline1\n# Another comment\nline2";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldBe("line1\nline2\n");
        }

        [Fact]
        public void Canonicalize_WithSemicolonComments_ShouldRemove()
        {
            // Arrange
            var input = "; This is a comment\nline1\n; Another comment\nline2";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldBe("line1\nline2\n");
        }

        [Fact]
        public void Canonicalize_WithDoubleSlashComments_ShouldRemove()
        {
            // Arrange
            var input = "// This is a comment\nline1\n// Another comment\nline2";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldBe("line1\nline2\n");
        }

        [Fact]
        public void Canonicalize_WithLeadingWhitespaceBeforeComments_ShouldRemove()
        {
            // Arrange
            var input = "    # Comment with leading spaces\nline1\n\t// Comment with leading tab\nline2";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldBe("line1\nline2\n");
        }

        [Fact]
        public void Canonicalize_WithMixedCommentMarkers_ShouldRemoveAll()
        {
            // Arrange
            var input = "# Hash comment\nline1\n; Semicolon comment\nline2\n// Slash comment\nline3";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldBe("line1\nline2\nline3\n");
        }

        [Fact]
        public void Canonicalize_WithCommentMarkersInMiddleOfLine_ShouldNotRemove()
        {
            // Arrange
            var input = "value # not a comment\nvalue // also not a comment";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldContain("value # not a comment");
            result.ShouldContain("value // also not a comment");
        }

        #endregion

        #region Trailing Newline Tests

        [Fact]
        public void Canonicalize_WithoutTrailingNewline_ShouldAddOne()
        {
            // Arrange
            var input = "line1\nline2";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldEndWith("\n");
            result.ShouldBe("line1\nline2\n");
        }

        [Fact]
        public void Canonicalize_WithTrailingNewline_ShouldKeepOne()
        {
            // Arrange
            var input = "line1\nline2\n";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldEndWith("\n");
            result.ShouldBe("line1\nline2\n");
        }

        [Fact]
        public void Canonicalize_WithMultipleTrailingNewlines_ShouldKeepOne()
        {
            // Arrange
            var input = "line1\nline2\n\n\n";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldEndWith("\n");
            result.ShouldNotEndWith("\n\n");
            result.ShouldBe("line1\nline2\n");
        }

        #endregion

        #region Indentation Preservation Tests

        [Fact]
        public void Canonicalize_WithPythonStyleIndentation_ShouldPreserve()
        {
            // Arrange
            var input = @"def function():
    if condition:
        do_something()
    return value";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldContain("    if condition:");
            result.ShouldContain("        do_something()");
            result.ShouldContain("    return value");
        }

        [Fact]
        public void Canonicalize_WithYamlStyleIndentation_ShouldPreserve()
        {
            // Arrange
            var input = @"parent:
  child1: value1
  child2:
    grandchild: value2";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldContain("  child1: value1");
            result.ShouldContain("  child2:");
            result.ShouldContain("    grandchild: value2");
        }

        [Fact]
        public void Canonicalize_WithTabIndentation_ShouldPreserve()
        {
            // Arrange
            var input = "level0\n\tlevel1\n\t\tlevel2";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            // Tabs are converted to spaces (count preserved)
            result.ShouldContain(" level1");
            result.ShouldContain("  level2");
        }

        [Fact]
        public void Canonicalize_WithMixedSpaceAndTabIndentation_ShouldPreserve()
        {
            // Arrange
            var input = "line1\n    spaces\n\ttab\n  \ttab and spaces";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            // Tabs are converted to spaces (count preserved)
            result.ShouldContain("    spaces");
            result.ShouldContain(" tab");
            result.ShouldContain("   tab and spaces");
        }

        #endregion

        #region Edge Cases and Complex Scenarios

        [Fact]
        public void Canonicalize_WithEmptyString_ShouldReturnSingleNewline()
        {
            // Arrange
            var input = "";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldBe("\n");
        }

        [Fact]
        public void Canonicalize_WithOnlyWhitespace_ShouldReturnSingleNewline()
        {
            // Arrange
            var input = "   \n\t\t\n     ";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldBe("\n");
        }

        [Fact]
        public void Canonicalize_WithOnlyComments_ShouldReturnSingleNewline()
        {
            // Arrange
            var input = "# Comment 1\n; Comment 2\n// Comment 3";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldBe("\n");
        }

        [Fact]
        public void Canonicalize_WithNullInput_ShouldThrow()
        {
            // Act & Assert
            Should.Throw<ArgumentNullException>(() => _canonicalizer.Canonicalize(null!));
        }

        [Fact]
        public void Canonicalize_WithSingleLine_ShouldAddTrailingNewline()
        {
            // Arrange
            var input = "single line";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldBe("single line\n");
        }

        [Fact]
        public void Canonicalize_WithComplexRealWorldExample_ShouldPreserveStructure()
        {
            // Arrange
            var input = @"# Configuration file
server:
    host: localhost
    port: 8080
    # Debug settings
    debug: true

database:
    url: postgres://localhost
    # Connection pool
    pool_size: 10";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldNotContain("# Configuration file");
            result.ShouldNotContain("# Debug settings");
            result.ShouldNotContain("# Connection pool");
            result.ShouldContain("server:");
            result.ShouldContain("    host: localhost");
            result.ShouldContain("    port: 8080");
            result.ShouldContain("    debug: true");
            result.ShouldContain("database:");
            result.ShouldContain("    url: postgres://localhost");
            result.ShouldContain("    pool_size: 10");
        }

        [Fact]
        public void Canonicalize_WithMarkdownExample_ShouldPreserveStructure()
        {
            // Arrange
            var input = @"# Title

## Section 1

Content with    multiple   spaces.

    Indented code block
    More indented code

## Section 2

More content.";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            // # lines are treated as comments and removed
            result.ShouldNotContain("# Title");
            result.ShouldNotContain("## Section 1");
            result.ShouldNotContain("## Section 2");
            result.ShouldContain("Content with multiple spaces.");
            result.ShouldContain("    Indented code block");
            result.ShouldContain("    More indented code");
            result.ShouldContain("More content.");
        }

        [Fact]
        public void Canonicalize_WithCsvExample_ShouldCollapseInternalWhitespace()
        {
            // Arrange
            var input = "name,    age,    city\nJohn,    25,    NYC\nJane,    30,    LA";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldBe("name, age, city\nJohn, 25, NYC\nJane, 30, LA\n");
        }

        [Fact]
        public void Canonicalize_ShouldBeIdempotent()
        {
            // Arrange
            var input = @"# Comment
line1    with   spaces
    indented line
line2";

            // Act
            var result1 = _canonicalizer.Canonicalize(input);
            var result2 = _canonicalizer.Canonicalize(result1);

            // Assert
            result1.ShouldBe(result2);
        }

        #endregion
    }
}
