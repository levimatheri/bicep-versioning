using Bicep.Versioning.Extensions;
using AwesomeAssertions;

namespace Bicep.Versioning.Tests;

[TestClass]
public class SemanticVersionRangeTests
{
    [TestMethod]
    [DynamicData(nameof(GetValidVersionRangesBasic))]
    public void ParseVersionRange_ValidRange_ReturnsExpectedProperties(
        string raw, int rangeCount)
    {
        TestParseValidVersion(raw, rangeCount);
    }

    [TestMethod]
    [DynamicData(nameof(GetInvalidVersionRangesBasic))]
    public void ParseVersionRange_InvalidRange_ThrowsFormatException(
       string raw)
    {
        TestParseInvalidVersion(raw);
    }

    [TestMethod]
    [DynamicData(nameof(GetSatisfiesRangeBasic))]
    public void SatisfiesRange(
        string range, string version, bool satisfies)
    {
        var versionRanges = SemanticVersionRange.Parse(range);
        var semanticVersion = SemanticVersion.Parse(version);

        var result = semanticVersion.Satisfies(versionRanges);

        result.Should().Be(satisfies);
    }

    [TestMethod]
    [DynamicData(nameof(GetSatisfiesRangeWithPrereleaseOrBuild))]
    public void SatisfiesRange_WithPrereleaseOrBuild(
        string range, string version, bool satisfies)
    {
        var versionRanges = SemanticVersionRange.Parse(range);
        var semanticVersion = SemanticVersion.Parse(version);

        var result = semanticVersion.Satisfies(versionRanges);

        result.Should().Be(satisfies);
    }

    private static void TestParseValidVersion(string versionRangeRaw, int rangeCount)
    {
        var versionRanges = SemanticVersionRange.Parse(versionRangeRaw);
        versionRanges.Should().HaveCount(rangeCount);
    }

    private static void TestParseInvalidVersion(string versionRangeRaw)
    {
        Action act = () => SemanticVersionRange.Parse(versionRangeRaw);
        act.Should().Throw<FormatException>();
    }

    private static IEnumerable<object[]> GetValidVersionRangesBasic =>
    [
        [ ">= 1.2.3", 1 ],
        [ "<=2.3.4", 1 ],
        [ "< 1.2.3", 1 ],
        [ ">2.3.4 ", 1 ],
        [ "1.2.3", 1 ],
        [ "=1.2.3", 1 ],
        [ "1.0.0", 1 ],
        [ ">= 1.2.3, < 2.0.0", 2 ],
        [ "~1.2.3", 1 ],
        [ "^1.2.3", 1 ],
        [ "^1.2, ^1", 2 ],
        [ "\t<1.2.3\t", 1]
    ];

    private static IEnumerable<object[]> GetInvalidVersionRangesBasic =>
    [
        [ ">= 1.x" ],
        [ "& 4.5.6" ],
        [ "!= 1.2.3" ],
        [ ">= 1.2.3 < 2" ]
    ];

    private static IEnumerable<object[]> GetSatisfiesRangeBasic =>
    [
        // Basic comparisons
        [ ">= 1.2.3", "1.2.3", true ],
        [ ">= 1.2.3", "1.2.2", false ],

        // Greater than
        [ "> 1.2.3", "1.2.4", true ],
        [ "> 1.2.3", "1.2.3", false ],

        // Less than or equal
        [ "<= 1.2.3", "1.2.3", true ],
        [ "<= 1.2.3", "1.2.4", false ],

        // Less than
        [ "< 1.2.3", "1.2.2", true ],
        [ "< 1.2.3", "1.2.3", false ],

        // Exact match
        [ "1.2.3", "1.2.3", true ],
        [ "1.2.3", "1.2.4", false ],
        [ "=1.2.3", "1.2.3", true ],
        [ "1.0.0", "1.0.0", true ],

        // Tilde ranges
        [ "~1.2.3", "1.2.3", true ],
        [ "~1.2.3", "1.3.0", false ],
        [ "~1.2.3", "1.2.4", true ],
        [ "~1.0.3", "1.0.9", true ],
        [ "~1.0.3", "1.1.0", false ],
        [ "~1.0.3", "2.0.0", false ],
        [ "~1.2", "1.2.2", true ],
        [ "~1.2", "1.3.0", false ],
        [ "~1.0", "1.3.4", true ],
        [ "~1", "2.0.0", false ],

        // Caret ranges
        [ "^1.2.3", "1.9.9", true ],
        [ "^1.2.3", "2.0.0", false ],
        [ "^1.2", "1.2.3", true ],
        [ "^1.2", "1.3.4", true ],
        [ "^1.2", "2.0.0", false ],
        [ "^1", "1.5", true ],
        [ "^0", "1.2", false ],
  

        // Multiple ranges (AND)
        [ ">= 1.2.3, < 2.0.0", "1.2.3", true ],
        [ ">= 1.2.3, < 2.0.0", "2.0.0", false ],
        [ "^1.2, ^2", "1.3.0", false ],
        [ "^1.2, ^1", "1.3.0", true ],
    ];


    private static IEnumerable<object[]> GetSatisfiesRangeWithPrereleaseOrBuild =>
    [
        // Prerelease versions
        [ ">= 1.2.3-alpha", "1.2.3-alpha", true ],
        [ ">= 1.2.3-alpha", "1.2.3-beta", true ],
        [ "<= 1.2.3-alpha", "1.2.3-alpha", true ],
        [ "<= 1.2.3-alpha", "1.2.3-beta", false ],

        // Prerelease vs. release
        [ ">= 1.2.3-alpha", "1.2.3", true ],
        [ "< 1.2.3", "1.2.3-alpha", true ],
        [ "<= 1.2.3", "1.2.3-alpha", true ],

        // Prerelease with different identifiers
        [ ">= 1.2.3-alpha", "1.2.3-alpha.1", true ],
        [ ">= 1.2.3-alpha.2", "1.2.3-alpha.1", false ],
        [ "< 1.2.3-alpha.2", "1.2.3-alpha.1", true ],

        // Build metadata ignored in precedence
        [ "1.2.3+build1", "1.2.3+build2", true ],
        [ "=1.2.3+build1", "1.2.3+build2", true ],
        [ "1.2.3-alpha+build1", "1.2.3-alpha+build2", true ],
        [ "=1.2.3-alpha+build1", "1.2.3-alpha+build2", true ],
        [ ">= 1.2.3-alpha+build1", "1.2.3-alpha+build2", true ],
        [ "<= 1.2.3-alpha+build1", "1.2.3-alpha+build2", true ],

        // tilde with prerelease
        [ "~1.2.3-beta.2", "1.2.3", true ],
        [ "~1.2.3-beta.2", "1.2.3-beta.4", true ],
        [ "~1.2.3-beta.2", "1.2.4-beta.2", false ],

        // caret with prerelease
        [ "^1.2.3-beta.2", "1.2.3", true ],
        [ "^1.2.3-beta.2", "1.2.3-beta.4", true ],
        [ "^1.2.3-beta.2", "1.2.4-beta.2", false ],
    ];
}
