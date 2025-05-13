using System.Text.RegularExpressions;

namespace Bicep.Versioning;

public partial class SemanticVersion
{
    public string Raw { get; }
    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }
    public IEnumerable<PrereleaseIdentifier> PrereleaseIdentifiers { get; }
    public IEnumerable<BuildIdentifier> BuildIdentifiers { get; }

    private SemanticVersion(
        string raw, 
        int major, 
        int minor, 
        int patch, 
        IEnumerable<PrereleaseIdentifier> prereleaseIdentifiers,
        IEnumerable<BuildIdentifier> buildIdentifiers)
    {
        Raw = raw;
        Major = major;
        Minor = minor;
        Patch = patch;
        PrereleaseIdentifiers = prereleaseIdentifiers;
        BuildIdentifiers = buildIdentifiers;
    }

    public static SemanticVersion Parse(string version)
    {
        var match = SemverRegex().Match(version);
        if (!match.Success)
        {
            throw new FormatException($"Malformed version: {version}");
        }

        var majorRaw = match.Groups["major"];
        var minorRaw = match.Groups["minor"];
        var patchRaw = match.Groups["patch"];
        var prereleaseRaw = match.Groups["prerelease"];
        var buildMetadataRaw = match.Groups["build"];
        
        // try-parse just in-case of overflows
        var major = majorRaw.Success ? int.Parse(majorRaw.Value) : 0;
        var minor = minorRaw.Success ? int.Parse(minorRaw.Value) : 0;
        var patch = patchRaw.Success ? int.Parse(patchRaw.Value) : 0;

        var prerelease =
            ParsePrerelease(prereleaseRaw.Success ? prereleaseRaw.Value : null);
        var buildMetadata = 
            ParseBuild(buildMetadataRaw.Success ? buildMetadataRaw.Value : null);
        
        return new SemanticVersion(
            version, 
            major,
            minor,
            patch,
            prerelease,
            buildMetadata);
    }
    
    private static IReadOnlyCollection<PrereleaseIdentifier> ParsePrerelease(string? prerelease)
    {
        if (string.IsNullOrEmpty(prerelease))
        {
            return Array.Empty<PrereleaseIdentifier>();
        }

        var prereleaseIdentifiers = prerelease.Split('.')
            .Select(x => new PrereleaseIdentifier(x))
            .ToArray();

        return prereleaseIdentifiers;
    }
    
    private static IReadOnlyCollection<BuildIdentifier> ParseBuild(string? build)
    {
        if (string.IsNullOrEmpty(build))
        {
            return Array.Empty<BuildIdentifier>();
        }

        var buildIdentifiers = build.Split('.')
            .Select(x => new BuildIdentifier(x))
            .ToArray();

        return buildIdentifiers;
    }
    
    [GeneratedRegex(@"^(?:[vV])?(?<major>0|[1-9]\d*)(?:\.(?<minor>0|[1-9]\d*))?(?:\.(?<patch>0|[1-9]\d*))?(?:-(?<prerelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+(?<build>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$")]
    private static partial Regex SemverRegex();
}