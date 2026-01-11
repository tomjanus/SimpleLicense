using Xunit;
using Shouldly;
using SimpleLicense.Core.Canonicalizers;

namespace SimpleLicense.Tests
{
    /// <summary>
    /// Tests for InpFileCanonicalizer - EPANET .inp file canonicalization
    /// </summary>
    public class InpFileCanonalizerTests
    {
        private readonly InpFileCanonicalizer _canonicalizer;

        public InpFileCanonalizerTests()
        {
            _canonicalizer = new InpFileCanonicalizer();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNoArguments_ShouldUseDefaultExtensions()
        {
            // Act
            var canonicalizer = new InpFileCanonicalizer();

            // Assert
            var extensions = canonicalizer.SupportedExtensions.ToList();
            extensions.ShouldContain(".inp");
            extensions.Count.ShouldBe(1);
        }

        [Fact]
        public void Constructor_WithCustomExtensions_ShouldUseProvidedExtensions()
        {
            // Arrange
            var customExtensions = new[] { ".custom", ".test" };

            // Act
            var canonicalizer = new InpFileCanonicalizer(customExtensions);

            // Assert
            var extensions = canonicalizer.SupportedExtensions.ToList();
            extensions.ShouldContain(".custom");
            extensions.ShouldContain(".test");
            extensions.ShouldNotContain(".inp");
        }

        [Fact]
        public void Constructor_WithNullExtensions_ShouldUseDefaultExtensions()
        {
            // Act
            var canonicalizer = new InpFileCanonicalizer(null);

            // Assert
            var extensions = canonicalizer.SupportedExtensions.ToList();
            extensions.ShouldContain(".inp");
        }

        [Fact]
        public void Constructor_WithEmptyExtensions_ShouldUseDefaultExtensions()
        {
            // Act
            var canonicalizer = new InpFileCanonicalizer(Array.Empty<string>());

            // Assert
            var extensions = canonicalizer.SupportedExtensions.ToList();
            extensions.ShouldContain(".inp");
        }

        [Fact]
        public void SupportedExtensions_ShouldBeCaseInsensitive()
        {
            // Arrange
            var canonicalizer = new InpFileCanonicalizer(new[] { ".INP", ".Inp" });

            // Act
            var extensions = canonicalizer.SupportedExtensions;

            // Assert
            extensions.ShouldContain(".inp");
        }

        #endregion

        #region Line Ending Normalization Tests

        [Fact]
        public void Canonicalize_WithWindowsLineEndings_ShouldNormalizeToUnix()
        {
            // Arrange
            var input = "[JUNCTIONS]\r\nJ1 100\r\nJ2 200";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldBe("[JUNCTIONS]\nJ1 100\nJ2 200\n");
        }

        [Fact]
        public void Canonicalize_WithMacLineEndings_ShouldNormalizeToUnix()
        {
            // Arrange
            var input = "[JUNCTIONS]\rJ1 100\rJ2 200";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldBe("[JUNCTIONS]\nJ1 100\nJ2 200\n");
        }

        [Fact]
        public void Canonicalize_WithMixedLineEndings_ShouldNormalizeToUnix()
        {
            // Arrange
            var input = "[JUNCTIONS]\r\nJ1 100\nJ2 200\rJ3 300";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldBe("[JUNCTIONS]\nJ1 100\nJ2 200\nJ3 300\n");
        }

        #endregion

        #region Section Header Tests

        [Fact]
        public void Canonicalize_WithSectionHeaders_ShouldUppercaseAndNormalize()
        {
            // Arrange
            var input = "[junctions]\nJ1 100\n[pipes]\nP1 J1 J2";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldContain("[JUNCTIONS]");
            result.ShouldContain("[PIPES]");
            result.ShouldNotContain("[junctions]", Case.Sensitive);
            result.ShouldNotContain("[pipes]", Case.Sensitive);
        }

        [Fact]
        public void Canonicalize_WithWhitespaceAroundSectionName_ShouldNormalize()
        {
            // Arrange
            var input = "[  junctions  ]\nJ1 100\n[  pipes  ]\nP1 J1 J2";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldContain("[JUNCTIONS]");
            result.ShouldContain("[PIPES]");
        }

        [Fact]
        public void Canonicalize_WithWhitespaceAroundBrackets_ShouldNormalize()
        {
            // Arrange
            var input = "  [junctions]  \nJ1 100\n\t[pipes]\t\nP1 J1 J2";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldContain("[JUNCTIONS]");
            result.ShouldContain("[PIPES]");
        }

        [Fact]
        public void Canonicalize_WithMixedCaseSectionNames_ShouldUppercase()
        {
            // Arrange
            var input = "[JuNcTiOnS]\nJ1 100\n[PiPeS]\nP1 J1 J2";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldContain("[JUNCTIONS]");
            result.ShouldContain("[PIPES]");
        }

        #endregion

        #region TITLE Section Tests

        [Fact]
        public void Canonicalize_WithTitleSection_ShouldRemoveEntireSection()
        {
            // Arrange
            var input = @"[TITLE]
My Network Title
Description Line 1
Description Line 2

[JUNCTIONS]
J1 100";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldNotContain("[TITLE]");
            result.ShouldNotContain("My Network Title");
            result.ShouldNotContain("Description Line 1");
            result.ShouldNotContain("Description Line 2");
            result.ShouldContain("[JUNCTIONS]");
            result.ShouldContain("J1 100");
        }

        [Fact]
        public void Canonicalize_WithTitleSectionAtEnd_ShouldRemove()
        {
            // Arrange
            var input = @"[JUNCTIONS]
J1 100

[TITLE]
My Network Title
Description";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldNotContain("[TITLE]");
            result.ShouldNotContain("My Network Title");
            result.ShouldNotContain("Description");
            result.ShouldContain("[JUNCTIONS]");
            result.ShouldContain("J1 100");
        }

        [Fact]
        public void Canonicalize_WithTitleSectionInMiddle_ShouldRemove()
        {
            // Arrange
            var input = @"[JUNCTIONS]
J1 100

[TITLE]
My Network Title

[PIPES]
P1 J1 J2";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldNotContain("[TITLE]");
            result.ShouldNotContain("My Network Title");
            result.ShouldContain("[JUNCTIONS]");
            result.ShouldContain("J1 100");
            result.ShouldContain("[PIPES]");
            result.ShouldContain("P1 J1 J2");
        }

        [Fact]
        public void Canonicalize_WithLowercaseTitleSection_ShouldRemove()
        {
            // Arrange
            var input = @"[title]
My Network Title

[JUNCTIONS]
J1 100";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldNotContain("title");
            result.ShouldNotContain("My Network Title");
            result.ShouldContain("[JUNCTIONS]");
        }

        [Fact]
        public void Canonicalize_WithMixedCaseTitleSection_ShouldRemove()
        {
            // Arrange
            var input = @"[TiTlE]
My Network Title

[JUNCTIONS]
J1 100";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldNotContain("TiTlE");
            result.ShouldNotContain("My Network Title");
            result.ShouldContain("[JUNCTIONS]");
        }

        #endregion

        #region Comment Handling Tests

        [Fact]
        public void Canonicalize_WithFullLineComments_ShouldRemove()
        {
            // Arrange
            var input = @"; This is a comment
[JUNCTIONS]
; Another comment
J1 100
; Final comment
J2 200";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldNotContain("; This is a comment");
            result.ShouldNotContain("; Another comment");
            result.ShouldNotContain("; Final comment");
            result.ShouldContain("[JUNCTIONS]");
            result.ShouldContain("J1 100");
            result.ShouldContain("J2 200");
        }

        [Fact]
        public void Canonicalize_WithInlineComments_ShouldRemoveCommentPart()
        {
            // Arrange
            var input = @"[JUNCTIONS]
J1 100 ; elevation in feet
J2 200 ; another junction";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldContain("J1 100");
            result.ShouldContain("J2 200");
            result.ShouldNotContain("; elevation in feet");
            result.ShouldNotContain("; another junction");
        }

        [Fact]
        public void Canonicalize_WithMixedFullLineAndInlineComments_ShouldRemoveAll()
        {
            // Arrange
            var input = @"; Full line comment
[JUNCTIONS]
J1 100 ; inline comment
; Another full line
J2 200";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldContain("[JUNCTIONS]");
            result.ShouldContain("J1 100");
            result.ShouldContain("J2 200");
            result.ShouldNotContain("; Full line comment");
            result.ShouldNotContain("; inline comment");
            result.ShouldNotContain("; Another full line");
        }

        [Fact]
        public void Canonicalize_WithCommentAtEndOfSectionHeader_ShouldRemove()
        {
            // Arrange
            var input = "[JUNCTIONS] ; Junction section\nJ1 100";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldContain("[JUNCTIONS]");
            result.ShouldNotContain("; Junction section");
        }

        #endregion

        #region Whitespace Handling Tests

        [Fact]
        public void Canonicalize_WithMultipleSpaces_ShouldCollapse()
        {
            // Arrange
            var input = "[JUNCTIONS]\nJ1    100    50    HEAD   1";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldContain("J1 100 50 HEAD 1");
            result.ShouldNotContain("    ");
        }

        [Fact]
        public void Canonicalize_WithTabs_ShouldCollapseToSingleSpace()
        {
            // Arrange
            var input = "[JUNCTIONS]\nJ1\t100\t\t50";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldContain("J1 100 50");
            result.ShouldNotContain("\t");
        }

        [Fact]
        public void Canonicalize_WithMixedSpacesAndTabs_ShouldCollapseToSingleSpace()
        {
            // Arrange
            var input = "[JUNCTIONS]\nJ1  \t  100 \t\t 50";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldContain("J1 100 50");
        }

        [Fact]
        public void Canonicalize_WithLeadingWhitespace_ShouldTrim()
        {
            // Arrange
            var input = "[JUNCTIONS]\n    J1 100\n\tJ2 200";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldContain("J1 100");
            result.ShouldContain("J2 200");
            result.ShouldNotContain("    J1");
            result.ShouldNotContain("\tJ2");
        }

        [Fact]
        public void Canonicalize_WithTrailingWhitespace_ShouldTrim()
        {
            // Arrange
            var input = "[JUNCTIONS]\nJ1 100   \nJ2 200\t\t";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldBe("[JUNCTIONS]\nJ1 100\nJ2 200\n");
        }

        [Fact]
        public void Canonicalize_WithEmptyLines_ShouldRemove()
        {
            // Arrange
            var input = "[JUNCTIONS]\n\nJ1 100\n\n\nJ2 200\n\n";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldBe("[JUNCTIONS]\nJ1 100\nJ2 200\n");
        }

        [Fact]
        public void Canonicalize_WithWhitespaceOnlyLines_ShouldRemove()
        {
            // Arrange
            var input = "[JUNCTIONS]\n   \nJ1 100\n\t\t\nJ2 200";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldBe("[JUNCTIONS]\nJ1 100\nJ2 200\n");
        }

        #endregion

        #region Trailing Newline Tests

        [Fact]
        public void Canonicalize_WithoutTrailingNewline_ShouldAddOne()
        {
            // Arrange
            var input = "[JUNCTIONS]\nJ1 100";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldEndWith("\n");
            result.ShouldBe("[JUNCTIONS]\nJ1 100\n");
        }

        [Fact]
        public void Canonicalize_WithTrailingNewline_ShouldKeepOne()
        {
            // Arrange
            var input = "[JUNCTIONS]\nJ1 100\n";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldEndWith("\n");
            result.ShouldBe("[JUNCTIONS]\nJ1 100\n");
        }

        [Fact]
        public void Canonicalize_WithMultipleTrailingNewlines_ShouldKeepOne()
        {
            // Arrange
            var input = "[JUNCTIONS]\nJ1 100\n\n\n";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldEndWith("\n");
            result.ShouldNotEndWith("\n\n");
            result.ShouldBe("[JUNCTIONS]\nJ1 100\n");
        }

        #endregion

        #region Edge Cases Tests

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
            var input = "; Comment 1\n; Comment 2\n; Comment 3";

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
        public void Canonicalize_WithOnlyTitleSection_ShouldReturnSingleNewline()
        {
            // Arrange
            var input = @"[TITLE]
My Network Title
Description";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldBe("\n");
        }

        #endregion

        #region Complex Real-World Scenarios

        [Fact]
        public void Canonicalize_WithCompleteInpFile_ShouldProcessCorrectly()
        {
            // Arrange
            var input = @"[TITLE]
Sample Network
This is a test network

; Network junctions
[JUNCTIONS]
;ID    Elev    Demand    Pattern
J1     100     50        1
J2     120     75        1 ; main junction
J3     110     0         ; zero demand

[PIPES]
;ID    Node1    Node2    Length    Diameter    Roughness
P1     J1       J2       1000      12          100
P2     J2       J3       1500      8           100 ; smaller pipe

[END]";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            // Title section should be removed
            result.ShouldNotContain("[TITLE]");
            result.ShouldNotContain("Sample Network");
            result.ShouldNotContain("This is a test network");

            // Section headers should be uppercase
            result.ShouldContain("[JUNCTIONS]");
            result.ShouldContain("[PIPES]");
            result.ShouldContain("[END]");

            // Comments should be removed
            result.ShouldNotContain("; Network junctions");
            result.ShouldNotContain(";ID    Elev    Demand    Pattern");
            result.ShouldNotContain("; main junction");
            result.ShouldNotContain("; zero demand");
            result.ShouldNotContain("; smaller pipe");

            // Data should be preserved with normalized whitespace
            result.ShouldContain("J1 100 50 1");
            result.ShouldContain("J2 120 75 1");
            result.ShouldContain("J3 110 0");
            result.ShouldContain("P1 J1 J2 1000 12 100");
            result.ShouldContain("P2 J2 J3 1500 8 100");
        }

        [Fact]
        public void Canonicalize_WithValuesContainingSpaces_ShouldPreserveSingleSpaces()
        {
            // Arrange
            // EPANET allows some values like "HEAD 1" to be two tokens
            var input = "[JUNCTIONS]\nJ1 100 50 HEAD 1";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldContain("HEAD 1");
            result.ShouldContain("J1 100 50 HEAD 1");
        }

        [Fact]
        public void Canonicalize_WithMultipleSections_ShouldProcessAll()
        {
            // Arrange
            var input = @"[JUNCTIONS]
J1 100

[RESERVOIRS]
R1 500

[TANKS]
T1 200 50 0 100

[PIPES]
P1 J1 R1 1000 12 100";

            // Act
            var result = _canonicalizer.Canonicalize(input);

            // Assert
            result.ShouldContain("[JUNCTIONS]");
            result.ShouldContain("[RESERVOIRS]");
            result.ShouldContain("[TANKS]");
            result.ShouldContain("[PIPES]");
            result.ShouldContain("J1 100");
            result.ShouldContain("R1 500");
            result.ShouldContain("T1 200 50 0 100");
            result.ShouldContain("P1 J1 R1 1000 12 100");
        }

        [Fact]
        public void Canonicalize_ShouldBeIdempotent()
        {
            // Arrange
            var input = @"; Comment
[TITLE]
My Title

[JUNCTIONS]
;Comment
J1    100    50 ; inline comment
J2     200    75

[PIPES]
P1   J1   J2   1000   12   100";

            // Act
            var result1 = _canonicalizer.Canonicalize(input);
            var result2 = _canonicalizer.Canonicalize(result1);

            // Assert
            result1.ShouldBe(result2);
        }

        [Fact]
        public void Canonicalize_WithDifferentFormats_ShouldProduceSameOutput()
        {
            // Arrange - Same logical content, different formatting
            var input1 = "[JUNCTIONS]\nJ1 100 50\nJ2 200 75";
            var input2 = "[junctions]\nJ1    100    50\nJ2    200    75";
            var input3 = "[JUNCTIONS]\n  J1\t100\t50  \n  J2\t200\t75  ";
            var input4 = "; Comment\n[JUNCTIONS]\nJ1 100 50 ; inline\nJ2 200 75";

            // Act
            var result1 = _canonicalizer.Canonicalize(input1);
            var result2 = _canonicalizer.Canonicalize(input2);
            var result3 = _canonicalizer.Canonicalize(input3);
            var result4 = _canonicalizer.Canonicalize(input4);

            // Assert
            result1.ShouldBe(result2);
            result2.ShouldBe(result3);
            result3.ShouldBe(result4);
        }

        #endregion
    }
}
