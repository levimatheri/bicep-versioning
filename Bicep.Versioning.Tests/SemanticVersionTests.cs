using FluentAssertions;

namespace Bicep.Versioning.Tests;

[TestClass]
public class SemanticVersionTests
{
    [TestMethod]
    [DynamicData(nameof(GetValidVersionsBasic))]
    public void Parse_ValidVersionBasic_ReturnsCorrectSemanticVersion(
        string raw, int major, int minor, int patch,
        IEnumerable<PrereleaseIdentifier> prerelease, IEnumerable<BuildIdentifier> build)
    {
        TestValidVersion(raw, major, minor, patch, prerelease, build);
    }
    
    [TestMethod]
    [DynamicData(nameof(GetValidVersionsWithVPrefix))]
    public void Parse_ValidVersionWithVPrefix_ReturnsCorrectSemanticVersion(
        string raw, int major, int minor, int patch,
        IEnumerable<PrereleaseIdentifier> prerelease, IEnumerable<BuildIdentifier> build)
    {
        TestValidVersion(raw, major, minor, patch, prerelease, build);
    }
    
    [TestMethod]
    [DynamicData(nameof(GetValidVersionsWithOmittedPatch))]
    public void Parse_ValidVersionWithOmittedPatch_ReturnsCorrectSemanticVersion(
        string raw, int major, int minor, int patch,
        IEnumerable<PrereleaseIdentifier> prerelease, IEnumerable<BuildIdentifier> build)
    {
        TestValidVersion(raw, major, minor, patch, prerelease, build);
    }
    
    [TestMethod]
    [DynamicData(nameof(GetValidVersionsWithOmittedMinorAndPatch))]
    public void Parse_ValidVersionWithOmittedMinorAndPatch_ReturnsCorrectSemanticVersion(
        string raw, int major, int minor, int patch,
        IEnumerable<PrereleaseIdentifier> prerelease, IEnumerable<BuildIdentifier> build)
    {
        TestValidVersion(raw, major, minor, patch, prerelease, build);
    }
    
    [TestMethod]
    [DynamicData(nameof(GetValidVersionsWithLeadingOrTrailingWhitespace))]
    public void Parse_ValidVersionWithLeadingOrTrailingWhitespace_ReturnsCorrectSemanticVersion(
        string raw, int major, int minor, int patch,
        IEnumerable<PrereleaseIdentifier> prerelease, IEnumerable<BuildIdentifier> build)
    {
        TestValidVersion(raw, major, minor, patch, prerelease, build);
    }
    
    [TestMethod]
    [DynamicData(nameof(GetInvalidVersionsBasic))] 
    public void Parse_InvalidVersion_ThrowsFormatException(string raw)
    {
        TestInvalidVersion(raw);
    }
    
    [TestMethod]
    [DynamicData(nameof(GetInvalidVersionsInvalidCharacters))] 
    public void Parse_InvalidVersionInvalidCharacters_ThrowsFormatException(string raw)
    {
        TestInvalidVersion(raw);
    }
    
    [TestMethod]
    [DynamicData(nameof(GetInvalidVersionsEmptyOrWhitespace))]
    public void Parse_InvalidVersionEmptyOrWhitespace_ThrowsFormatException(string raw)
    {
        TestInvalidVersion(raw);
    }
    
    [TestMethod]
    [DynamicData(nameof(GetInvalidVersionsFourthNumber))]
    public void Parse_InvalidVersionFourthNumber_ThrowsFormatException(string raw)
    {
        TestInvalidVersion(raw);
    }
    
    [TestMethod]
    [DynamicData(nameof(GetInvalidVersionsTrailingDot))]
    public void Parse_InvalidVersionTrailingDot_ThrowsFormatException(string raw)
    {
        TestInvalidVersion(raw);
    }
    
    [TestMethod]
    [DynamicData(nameof(GetInvalidVersionsInvalidSeparator))]
    public void Parse_InvalidVersionInvalidSeparator_ThrowsFormatException(string raw)
    {
        TestInvalidVersion(raw);
    }
    
    private void TestValidVersion(string versionRaw, int major, int minor, int patch,
        IEnumerable<PrereleaseIdentifier> prerelease, IEnumerable<BuildIdentifier> build)
    {
        var semanticVersion = SemanticVersion.Parse(versionRaw);
        semanticVersion.Major.Should().Be(major);
        semanticVersion.Minor.Should().Be(minor);
        semanticVersion.Patch.Should().Be(patch);
        semanticVersion.PrereleaseIdentifiers.Should().BeEquivalentTo(prerelease);
        semanticVersion.BuildIdentifiers.Should().BeEquivalentTo(build);
        semanticVersion.Raw.Should().Be(versionRaw);
    }
    
    private void TestInvalidVersion(string versionRaw)
    {
        var act = () => SemanticVersion.Parse(versionRaw);
        act.Should().Throw<FormatException>()
            .WithMessage($"Malformed version: {versionRaw}");
    }
    
    // test cases from https://semver.org/#is-there-a-suggested-regular-expression-regex-to-check-a-semver-string
    private static IEnumerable<object[]> GetValidVersionsBasic =>
    [
        ["0.0.4", 0, 0, 4, Pre(), Build()],
        ["1.2.3", 1, 2, 3, Pre(), Build()],
        ["1.1.7", 1, 1, 7, Pre(), Build()],
        ["1.1.2-prerelease+meta", 1, 1, 2, Pre("prerelease"), Build("meta")],
        ["1.1.2+meta", 1, 1, 2, Pre(), Build("meta")],
        ["1.1.2+meta-valid", 1, 1, 2, Pre(), Build("meta-valid")],
        ["1.0.0-alpha", 1, 0, 0, Pre("alpha"), Build()],
        ["1.0.0-beta", 1, 0, 0, Pre("beta"), Build()],
        ["1.0.0-alpha.beta", 1, 0, 0, Pre("alpha", "beta"), Build()],
        ["1.0.0-alpha.beta.1", 1, 0, 0, Pre("alpha", "beta", "1"), Build()],
        ["1.0.0-alpha.1", 1, 0, 0, Pre("alpha", "1"), Build()],
        ["1.0.0-alpha0.valid", 1, 0, 0, Pre("alpha0", "valid"), Build()],
        ["1.0.0-alpha.0valid", 1, 0, 0, Pre("alpha", "0valid"), Build()],
        ["1.0.0-alpha-a.b-c-somethinglong+build.1-aef.1-its-okay", 1, 0, 0, Pre("alpha-a", "b-c-somethinglong"), Build("build", "1-aef", "1-its-okay")],
        ["1.0.0-rc.1+build.1", 1, 0, 0, Pre("rc", "1"), Build("build", "1")],
        ["2.0.0-rc.1+build.123", 2, 0, 0, Pre("rc", "1"), Build("build", "123")],
        ["1.2.3-beta", 1, 2, 3, Pre("beta"), Build()],
        ["10.2.3-DEV-SNAPSHOT", 10, 2, 3, Pre("DEV-SNAPSHOT"), Build()],
        ["1.2.3-SNAPSHOT-123", 1, 2, 3, Pre("SNAPSHOT-123"), Build()],
        ["2.0.0+build.1848", 2, 0, 0, Pre(), Build("build", "1848")],
        ["2.0.1-alpha.1227", 2, 0, 1, Pre("alpha", "1227"), Build()],
        ["1.0.0-alpha+beta", 1, 0, 0, Pre("alpha"), Build("beta")],
        ["1.2.3----RC-SNAPSHOT.12.9.1--.12+788", 1, 2, 3, Pre("---RC-SNAPSHOT", "12", "9", "1--", "12"), Build("788")],
        ["1.2.3----R-S.12.9.1--.12+meta", 1, 2, 3, Pre("---R-S", "12", "9", "1--", "12"), Build("meta")],
        ["1.2.3----RC-SNAPSHOT.12.9.1--.12", 1, 2, 3, Pre("---RC-SNAPSHOT", "12", "9", "1--", "12"), Build()],
        ["1.0.0+0.build.1-rc.10000aaa-kk-0.1", 1, 0, 0, Pre(), Build("0", "build", "1-rc", "10000aaa-kk-0", "1")],
        ["1.0.0-0A.is.legal", 1, 0, 0, Pre("0A", "is", "legal"), Build()]
    ];

    private static IEnumerable<object[]> GetValidVersionsWithVPrefix =>
    [
        ["v14.5.6", 14, 5, 6, Pre(), Build()],
        ["V14.5.6", 14, 5, 6, Pre(), Build()]
    ];

    private static IEnumerable<object[]> GetValidVersionsWithOmittedPatch =>
    [
        ["1.2", 1, 2, 0, Pre(), Build()],
        ["5.16+build", 5, 16, 0, Pre(), Build("build")],
        ["1.7-alpha.43", 1, 7, 0, Pre("alpha", "43"), Build()],
    ];
    
    private static IEnumerable<object[]> GetValidVersionsWithOmittedMinorAndPatch =>
    [
        ["1", 1, 0, 0, Pre(), Build()],
        ["5+build", 5, 0, 0, Pre(), Build("build")],
        ["1-alpha.43", 1, 0, 0, Pre("alpha", "43"), Build()],
    ];
    
    private static IEnumerable<object[]> GetValidVersionsWithLeadingOrTrailingWhitespace =>
    [
        [" 1.2.3", 1, 2, 3, Pre(), Build()],
        ["1.2.3 ", 1, 2, 3, Pre(), Build()],
        [" 1.2.3 ", 1, 2, 3, Pre(), Build()],
        ["\t1.2.3-a", 1, 2, 3, Pre("a"), Build()],
        ["1.2.3+b\t", 1, 2, 3, Pre(), Build("b")],
        ["\t1.2.3\t", 1, 2, 3, Pre(), Build()]
    ];

    // test cases from https://semver.org/#is-there-a-suggested-regular-expression-regex-to-check-a-semver-string
    private static IEnumerable<object[]> GetInvalidVersionsBasic =>
    [
        ["1.2.3-0123"],
        ["1.2.3-0123.0123"],
        ["1.1.2+.123"],
        ["+invalid"],
        ["-invalid"],
        ["-invalid+invalid"],
        ["-invalid.01"],
        ["alpha"],
        ["alpha.beta"],
        ["alpha.beta.1"],
        ["alpha.1"],
        ["alpha+beta"],
        ["alpha_beta"],
        ["alpha."],
        ["alpha.."],
        ["beta"],
        ["1.0.0-alpha_beta"],
        ["-alpha."],
        ["1.0.0-alpha.."],
        ["1.0.0-alpha..1"],
        ["1.0.0-alpha...1"],
        ["1.0.0-alpha....1"],
        ["1.0.0-alpha.....1"],
        ["1.0.0-alpha......1"],
        ["1.0.0-alpha.......1"],
        ["01.1.1"],
        ["1.01.1"],
        ["1.1.01"],
        ["1.2.3.DEV"],
        ["1.2.31.2.3----RC-SNAPSHOT.12.09.1--..12+788"],
        ["-1.0.3-gamma+b7718"],
        ["+justmeta"],
        ["9.8.7+meta+meta"],
        ["9.8.7-whatever+meta+meta"]
    ];

    private static IEnumerable<object[]> GetInvalidVersionsInvalidCharacters =>
    [
        ["1@.2.3"],
        ["1.2@.3"],
        ["1.2.3@"],
        ["1.2.3@-alpha+build"],
        ["1.2.3-💪+b"],
        ["1.2.3-a+💪"],
        ["1.2.3-á"],
        ["1.2.3+á"]
    ];

    private static IEnumerable<object[]> GetInvalidVersionsEmptyOrWhitespace =>
    [
        [" "],
        ["\t"],
        ["\n"],
        [""],
        ["\r"],
        ["\r\n"],
        ["1.  2.3"],
        ["1.2   .3-a+b"],
        ["1.2.3 -a+build"],
        ["1.2.3-alp ha+build"],
        ["1.2.3-alpha +build"],
        ["1.2.3-alpha+ build"],
        ["1.2.3-alpha+bu ild"],
        ["1.2.3-alpha+build .2"]
    ];

    private static IEnumerable<object[]> GetInvalidVersionsFourthNumber =>
    [
        ["1.2.3.4"],
        ["1.2.3.0-alpha"],
        ["1.2.3.0-gamma+b23"],
    ];
    
    private static IEnumerable<object[]> GetInvalidVersionsTrailingDot =>
    [
        ["1.2.3."],
        ["1.2."],
        ["1."],
    ];
    
    private static IEnumerable<object[]> GetInvalidVersionsInvalidSeparator =>
    [
        ["1.3.5.alpha"],
        ["1.2.3alpha"],
        ["1.2.3-alpha,beta"],
    ];
    
    private static IEnumerable<PrereleaseIdentifier> Pre(params IEnumerable<string> value)
        => value.Select(i => new PrereleaseIdentifier(i));
    private static IEnumerable<BuildIdentifier> Build(params IEnumerable<string> value)
        => value.Select(i => new BuildIdentifier(i));
}