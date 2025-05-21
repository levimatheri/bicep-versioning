using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Bicep.Versioning;

public partial class SemanticVersion
{
    public string Raw { get; }
    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }
    public string? Prerelease { get; }
    public string? BuildMetadata { get; }
    public IEnumerable<PrereleaseIdentifier>? PrereleaseIdentifiers { get; }
    public IEnumerable<BuildIdentifier>? BuildIdentifiers { get; }

    internal SemanticVersion(
        string raw, 
        int major, 
        int minor, 
        int patch, 
        string? prerelease = null,
        string? buildMetadata = null,
        IEnumerable<PrereleaseIdentifier>? prereleaseIdentifiers = null,
        IEnumerable<BuildIdentifier>? buildIdentifiers = null)
    {
        Raw = raw;
        Major = major;
        Minor = minor;
        Patch = patch;
        Prerelease = prerelease;
        BuildMetadata = buildMetadata;
        PrereleaseIdentifiers = prereleaseIdentifiers;
        BuildIdentifiers = buildIdentifiers;
    }

    /// <summary>
    /// Parses a semantic version string into a SemanticVersion object.
    /// The version string must be in the format: major.minor.patch[-prerelease][+build]
    /// </summary>
    /// <param name="version">The version string to parse.</param>
    /// <returns>A SemanticVersion object representing the parsed version.</returns>
    /// <exception cref="FormatException">Thrown when the version string is not in a valid format.</exception>
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

        var major = majorRaw.Success ? int.Parse(majorRaw.Value) : 0;
        var minor = minorRaw.Success ? int.Parse(minorRaw.Value) : 0;
        var patch = patchRaw.Success ? int.Parse(patchRaw.Value) : 0;

        var prerelease = prereleaseRaw.Success ? prereleaseRaw.Value : null;
        var buildMetadata = buildMetadataRaw.Success ? buildMetadataRaw.Value : null;

        var prereleaseIdentifiers = ParsePrerelease(prerelease);
        var buildMetadataIdentifiers = ParseBuildMetadata(buildMetadata);

        return new SemanticVersion(
            version,
            major,
            minor,
            patch,
            prerelease,
            buildMetadata,
            prereleaseIdentifiers,
            buildMetadataIdentifiers);
    }

    /// <summary>
    /// Compares two SemanticVersion objects for equality.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool Equals(SemanticVersion? left, SemanticVersion? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }
        if (left is null || right is null)
        {
            return false;
        }
        return left.Equals(right);
    }

    public override string ToString()
    {
        var version = $"{Major}.{Minor}.{Patch}";
        if (!string.IsNullOrEmpty(Prerelease))
        {
            version += $"-{Prerelease}";
        }
        if (!string.IsNullOrEmpty(BuildMetadata))
        {
            version += $"+{BuildMetadata}";
        }
        return version;
    }

    public override bool Equals(object? obj)
        => obj is SemanticVersion other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(Major, Minor, Patch, Prerelease, BuildMetadata);
    public bool LessThan(SemanticVersion other)
        => this.CompareTo(other) < 0;
    public bool GreaterThan(SemanticVersion other)
        => this.CompareTo(other) > 0;
    public bool LessThanOrEqual(SemanticVersion other)
        => this.CompareTo(other) <= 0;
    public bool GreaterThanOrEqual(SemanticVersion other)
        => this.CompareTo(other) >= 0;

// TODO: Revisit this
    public bool SatisfiedTildeRange(SemanticVersion other)
    {
        // ~1.2.3 := >=1.2.3 <1.3.0
        if (!this.GreaterThanOrEqual(other))
            return false;

        if (this.Major != other.Major)
            return false;

        // If other.Minor is specified, upper bound is next minor
        int upperMinor = other.Minor + 1;
        var upperBoundRaw = $"{other.Major}.{upperMinor}.0";
        return this.Minor == other.Minor && this.LessThan(new SemanticVersion(
            upperBoundRaw, other.Major, upperMinor, 0));
    }

    public bool SatisfiesCaretRange(SemanticVersion other)
    {
        // ^1.2.3 := >=1.2.3 <2.0.0
        // ^0.2.3 := >=0.2.3 <0.3.0
        // ^0.0.3 := >=0.0.3 <0.0.4
        if (!this.GreaterThanOrEqual(other))
            return false;

        if (other.Major > 0)
        {
            // < next major
            var upperBoundRaw = $"{other.Major + 1}.0.0";
            return this.LessThan(new SemanticVersion(
                upperBoundRaw, other.Major + 1, 0, 0));
        }
        if (other.Minor > 0)
        {
            // < next minor
            var upperBoundRaw = $"0.{other.Minor + 1}.0";
            return this.Major == 0 &&
                this.LessThan(new SemanticVersion(
                    upperBoundRaw, 0, other.Minor + 1, 0));
        }
        // < next patch
        var patchUpperBoundRaw = $"0.0.{other.Patch + 1}";
        return this.Major == 0 && this.Minor == 0 &&
            this.LessThan(new SemanticVersion(
                patchUpperBoundRaw, 0, 0, other.Patch + 1));
    }

    public int CompareTo(SemanticVersion other)
    {
        var majorComparison = Major.CompareTo(other.Major);
        if (majorComparison != 0)
        {
            return majorComparison;
        }

        var minorComparison = Minor.CompareTo(other.Minor);
        if (minorComparison != 0)
        {
            return minorComparison;
        }

        var patchComparison = Patch.CompareTo(other.Patch);
        if (patchComparison != 0)
        {
            return patchComparison;
        }

        var thisHasPrerelease = PrereleaseIdentifiers?.Any() ?? false;
        var otherHasPrerelease = other.PrereleaseIdentifiers?.Any() ?? false;
        if (!thisHasPrerelease && !otherHasPrerelease)
        {
            return 0; // Both are release versions
        }

        if (!thisHasPrerelease)
        {
            return 1; // Release > Pre-release
        }

        if (!otherHasPrerelease)
        {
            return -1; // Pre-release < Release
        }

        return ComparePrereleases(other.PrereleaseIdentifiers!);
    }

    public bool Equals(SemanticVersion? other)
    {
        if (other is null)
        {
            return false;
        }

        return Major == other.Major &&
               Minor == other.Minor &&
               Patch == other.Patch &&
               string.Equals(Prerelease, other.Prerelease, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(BuildMetadata, other.BuildMetadata, StringComparison.OrdinalIgnoreCase);
    }

    private int ComparePrereleases(IEnumerable<PrereleaseIdentifier> other)
    {
        using var thisEnumerator = PrereleaseIdentifiers!.GetEnumerator();
        using var otherEnumerator = other.GetEnumerator();

        while (thisEnumerator.MoveNext() && otherEnumerator.MoveNext())
        {
            var thisIdentifier = thisEnumerator.Current;
            var otherIdentifier = otherEnumerator.Current;

            var comparison = thisIdentifier.CompareTo(otherIdentifier);
            if (comparison != 0)
            {
                return comparison;
            }
        }

        if (thisEnumerator.MoveNext())
        {
            return 1; // This has more identifiers
        }

        if (otherEnumerator.MoveNext())
        {
            return -1; // Other has more identifiers
        }

        return 0; // Both have the same number of identifiers
    }
    
    private static PrereleaseIdentifier[] ParsePrerelease(string? prerelease)
    {
        if (string.IsNullOrEmpty(prerelease))
        {
            return [];
        }

        var prereleaseIdentifiers = prerelease.Split('.')
            .Select(x => new PrereleaseIdentifier(x))
            .ToArray();

        return prereleaseIdentifiers;
    }
    
    private static BuildIdentifier[] ParseBuildMetadata(string? build)
    {
        if (string.IsNullOrEmpty(build))
        {
            return [];
        }

        var buildIdentifiers = build.Split('.')
            .Select(x => new BuildIdentifier(x))
            .ToArray();

        return buildIdentifiers;
    }

    internal const string SemVerRegexPattern = @"^\s*(?<major>0|[1-9]\d*)(?:\.(?<minor>0|[1-9]\d*))?(?:\.(?<patch>0|[1-9]\d*))?(?:-(?<prerelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+(?<build>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?\s*$";

    [GeneratedRegex(SemVerRegexPattern)]
    private static partial Regex SemverRegex();
}