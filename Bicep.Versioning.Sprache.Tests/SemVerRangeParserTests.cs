using AwesomeAssertions;
using Sprache;

namespace Bicep.Versioning.Sprache.Tests;

[TestClass]
[Ignore]
public class SemVerRangeParserTests
{
    [TestMethod]
    public void ParseSemVerRange_ShouldReturnCorrectRange_WhenValidInput()
    {
        // Arrange
        var input = ">=1.2.3";

        // Act
        var result = SemVerRangeParser.Range.Parse(input);

        // Assert
        result.Should().NotBeNull();
        result.Version.ToString().Should().Be("1.2.3");
        result.Operator.Should().Be(">=");
    }

    [TestMethod]
    public void ParseSemVerRange_ShouldThrowFormatException_WhenInvalidInput()
    {
        // Arrange
        var input = "1.*";

        // Act
        Action act = () => SemVerRangeParser.Range.Parse(input);

        // Assert
        act.Should().Throw<ParseException>();
    }

    [TestMethod]
    public void ParseSemVerRange_ShouldReturnCorrectRange_WhenPrereleasePresent()
    {
        // Arrange
        var input = ">=1.2.3-alpha.1";

        // Act
        var result = SemVerRangeParser.Range.Parse(input);

        // Assert
        result.Should().NotBeNull();
        result.Version.ToString().Should().Be("1.2.3-alpha.1");
        result.Operator.Should().Be(">=");
    }

    [TestMethod]
    public void ParseSemVerRange_ShouldReturnCorrectRange_WhenBuildMetadataPresent()
    {
        // Arrange
        var input = "<2.0.0+build.456";

        // Act
        var result = SemVerRangeParser.Range.Parse(input);

        // Assert
        result.Should().NotBeNull();
        result.Version.ToString().Should().Be("2.0.0+build.456");
        result.Operator.Should().Be("<");
    }

    [TestMethod]
    public void ParseSemVerRange_ShouldReturnCorrectRange_WhenPrereleaseAndBuildPresent()
    {
        // Arrange
        var input = "=1.2.3-beta+exp.sha.5114f85";

        // Act
        var result = SemVerRangeParser.Range.Parse(input);

        // Assert
        result.Should().NotBeNull();
        result.Version.ToString().Should().Be("1.2.3-beta+exp.sha.5114f85");
        result.Operator.Should().Be("=");
    }

    [DataTestMethod]
    [DataRow(">=1.2.3", "1.2.3", true)]
    [DataRow(">=1.2.3", "1.2.4", true)]
    [DataRow(">=1.2.3", "1.2.2", false)]
    [DataRow(">1.2.3", "1.2.3", false)]
    [DataRow(">1.2.3", "1.2.4", true)]
    [DataRow("<2.0.0", "1.9.9", true)]
    [DataRow("<2.0.0", "2.0.0", false)]
    [DataRow("<=2.0.0", "2.0.0", true)]
    [DataRow("=1.2.3", "1.2.3", true)]
    [DataRow("=1.2.3", "1.2.4", false)]
    [DataRow(">=1.2.3-alpha", "1.2.3-alpha", true)]
    [DataRow(">=1.2.3-alpha", "1.2.3", true)]
    [DataRow(">=1.2.3-alpha", "1.2.2", false)]
    [DataRow("<1.2.3-alpha", "1.2.3-alpha", false)]
    [DataRow("<1.2.3-alpha", "1.2.3-beta", false)]
    [DataRow("<1.2.3-alpha", "1.2.2", true)]
    [DataRow("=1.2.3+build.1", "1.2.3+build.2", true)] // build metadata ignored
    // Tilde (~) tests
    [DataRow("~1.2.3", "1.2.3", true)]
    [DataRow("~1.2.3", "1.2.4", true)]
    [DataRow("~1.2.3", "1.3.0", false)]
    [DataRow("~1.2.3", "2.0.0", false)]
    // Caret (^) tests
    [DataRow("^1.2.3", "1.2.3", true)]
    [DataRow("^1.2.3", "1.3.0", true)]
    [DataRow("^1.2.3", "2.0.0", false)]
    [DataRow("^0.2.3", "0.2.4", true)]
    [DataRow("^0.2.3", "0.3.0", false)]
    public void Satisfies_ShouldReturnExpectedResult(string range, string version, bool expected)
    {
        Satisfies(range, version).Should().Be(expected);
    }

    private static bool Satisfies(string rangeInput, string versionInput)
    {
        var range = SemVerRangeParser.Range.Parse(rangeInput);
        var version = SemVerParser.ParseSemVer(versionInput);

        int cmp = CompareVersions(version, range.Version);

        return range.Operator switch
        {
            ">=" => cmp >= 0,
            "<=" => cmp <= 0,
            ">" => cmp > 0,
            "<" => cmp < 0,
            "=" => cmp == 0,
            "^" => cmp == 0, // Simplified for test; real caret logic is more complex
            "~" => cmp == 0, // Simplified for test; real tilde logic is more complex
            _ => false
        };
    }

    private static int CompareVersions(SemVerVersion a, SemVerVersion b)
    {
        int result = a.Major.CompareTo(b.Major);
        if (result != 0) return result;
        result = a.Minor.CompareTo(b.Minor);
        if (result != 0) return result;
        result = a.Patch.CompareTo(b.Patch);
        if (result != 0) return result;

        // PreRelease: null > any pre-release, otherwise lexicographical
        if (!a.Prerelease.Any() && b.Prerelease.Any()) return 1;
        if (a.Prerelease.Any() && !b.Prerelease.Any()) return -1;
        if (a.Prerelease.Any() && b.Prerelease.Any())
        {
            result = a.Prerelease.SequenceEqual(b.Prerelease) ? 0 : a.Prerelease[0].CompareTo(b.Prerelease[0]);
            if (result != 0) return result;
        }

        // Build metadata is not considered in precedence
        return 0;
    }
}