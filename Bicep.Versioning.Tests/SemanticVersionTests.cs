using FluentAssertions;

namespace Bicep.Versioning.Tests;

[TestClass]
public class SemanticVersionTests
{
    [TestMethod]
    [DynamicData(nameof(GetValidVersions))]
    public void Parse_ValidVersion_ReturnsCorrectSemanticVersion(
        string raw, ulong major, ulong minor, ulong patch,
        IEnumerable<PrereleaseIdentifer> prerelease, IEnumerable<BuildIdentifier> build)
    {
        var semanticVersion = SemanticVersion.Parse(raw);
        semanticVersion.Major.Should().Be(major);
        semanticVersion.Minor.Should().Be(minor);
        semanticVersion.Patch.Should().Be(patch);
        semanticVersion.PrereleaseIdentifiers.Should().BeEquivalentTo(prerelease);
        semanticVersion.BuildIdentifiers.Should().BeEquivalentTo(build);
        semanticVersion.Raw.Should().Be(raw);
    }
    
    public static IEnumerable<object[]> GetValidVersions
    {
        get
        {
            yield return ["1.0.0", 1UL, 0UL, 0UL, Array.Empty<PrereleaseIdentifer>(), Array.Empty<BuildIdentifier>()];
            yield return ["1.2.3", 1UL, 2UL, 3UL, Array.Empty<PrereleaseIdentifer>(), Array.Empty<BuildIdentifier>()];
            yield return ["1.2.3-alpha", 1UL, 2UL, 3UL, new[] { new PrereleaseIdentifer("alpha") }, Array.Empty<BuildIdentifier>()];
            yield return ["1.2.3+build", 1UL, 2UL, 3UL, Array.Empty<PrereleaseIdentifer>(), new[] { new BuildIdentifier("build") }];
            yield return ["1.2.3-alpha+build", 1UL, 2UL, 3UL, new[] { new PrereleaseIdentifer("alpha") }, new[] { new BuildIdentifier("build") }];
        }
    }
}