using Bicep.Versioning.Extensions;
using FluentAssertions;

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

    private static void TestParseValidVersion(string versionRangeRaw, int rangeCount)
    {
        var versionRanges = SemanticVersionRange.Parse(versionRangeRaw);
        versionRanges.Should().HaveCountGreaterThan(0);
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
        [ ">= 1.2.3, < 2.0.0", 2 ],
        [ "<=2.3", 1 ],
        [ "> 2", 1 ],
        [ "~1.2.3", 1 ],
        [ "^1.2.3", 1 ],
        [ "^1.2, ^1", 2 ]
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
        [ ">= 1.2.3", "1.2.3", true ],
        [ ">= 1.2.3", "1.2.5", true ],
        [ ">= 1.2.3", "2.0.0", true ],
        [ ">= 1.2.3", "1.1.5", false ],
        [ ">= 1.2.3", "0.2.3", false ],
        [ ">= 1.2.3", "1.2.2", false ],
        [ ">= 1.2", "1.2.3", true ],
        [ ">= 1", "1.2.3", true ],
    ];
}
