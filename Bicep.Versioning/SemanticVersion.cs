using System.Text.RegularExpressions;

namespace Bicep.Versioning;

public partial class SemanticVersion : IEquatable<SemanticVersion>, IComparable<SemanticVersion>
{
    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }
    public IReadOnlyList<PrereleaseIdentifier>? PrereleaseIdentifiers { get; }
    public IReadOnlyList<BuildIdentifier>? BuildIdentifiers { get; }
    
    private string? Prerelease { get; }
    private string? BuildMetadata { get; }

    private SemanticVersion(
        int major, 
        int minor, 
        int patch, 
        string? prerelease = null,
        string? buildMetadata = null,
        IReadOnlyList<PrereleaseIdentifier>? prereleaseIdentifiers = null,
        IReadOnlyList<BuildIdentifier>? buildIdentifiers = null)
    {
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

    public bool SatisfiesTilde(SemanticVersion other)
    {
        if (!this.GreaterThanOrEqual(other))
            return false;

        if (this.Major != other.Major)
        {
            // If the major version differs, the version doesn't satisfy the range
            return false;
        }

        var upperMinor = other.Minor > 0 || other.Patch > 0 ? other.Minor + 1 : 0;
        var upperMajor = other.Major + 1;

        SemanticVersion upperBound;
        if (other.Minor > 0 || other.Patch > 0)
        {
            // If other.Patch is specified, upper bound is next minor
            var upperBoundRaw = $"{other.Major}.{upperMinor}.0";
            upperBound = new SemanticVersion(other.Major, upperMinor, 0);
        }
        else
        {
            // If other.Minor is 0, upper bound is next major
            var upperMajorBoundRaw = $"{upperMajor}.0.0";
            upperBound = new SemanticVersion(upperMajor, 0, 0);
        }

        // If 'other' is a prerelease, only allow prereleases of the same [major, minor, patch] tuple
        if (other.PrereleaseIdentifiers?.Any() is false)
        {
            return this.LessThan(upperBound);
        }
        
        if (this.Major == other.Major && this.Minor == other.Minor && this.Patch == other.Patch)
        {
            if (this.PrereleaseIdentifiers?.Any() == true)
            {
                return this.ComparePrereleases(other.PrereleaseIdentifiers!) >= 0;
            }

            // Release version of the same [major, minor, patch] tuple is allowed
            return true;
        }

        // Not the same [major, minor, patch] tuple
        return false;
    }

    public bool SatisfiesCaret(SemanticVersion other)
    {
        if (!this.GreaterThanOrEqual(other))
            return false;

        if (other.Major > 0)
        {
            // < next major
            var upperBoundRaw = $"{other.Major + 1}.0.0";
            var upperBound = new SemanticVersion(other.Major + 1, 0, 0);

            if (!this.LessThan(upperBound))
            {
                return false;
            }
            
            // If other is a prerelease, only allow prereleases of the same [major, minor, patch] tuple
            if (other.PrereleaseIdentifiers?.Any() == true)
            {
                if (this.Major == other.Major && this.Minor == other.Minor && this.Patch == other.Patch)
                {
                    if (this.PrereleaseIdentifiers?.Any() == true)
                    {
                        return this.ComparePrereleases(other.PrereleaseIdentifiers!) >= 0;
                    }

                    // Release version of the same [major, minor, patch] tuple is allowed
                    return true;
                }

                return false;
            }

            // If other is not a prerelease, allow any version with the same major
            return this.Major == other.Major;
        }

        if (other.Minor > 0)
        {
            // < next minor
            var upperBoundRaw = $"0.{other.Minor + 1}.0";
            var upperBound = new SemanticVersion(0, other.Minor + 1, 0);

            if (!this.LessThan(upperBound))
            {
                return false;
            }
            
            if (other.PrereleaseIdentifiers?.Any() == true)
            {
                if (this.Major == 0 && this.Minor == other.Minor && this.Patch == other.Patch)
                {
                    if (this.PrereleaseIdentifiers?.Any() == true)
                    {
                        return this.ComparePrereleases(other.PrereleaseIdentifiers!) >= 0;
                    }
                    
                    return true;
                }
                
                return false;
            }
            
            return this.Major == 0 && this.Minor == other.Minor;
        }

        // < next patch
        var patchUpperBoundRaw = $"0.0.{other.Patch + 1}";
        var patchUpperBound = new SemanticVersion(0, 0, other.Patch + 1);

        if (!this.LessThan(patchUpperBound))
        {
            return false;
        }
        
        if (other.PrereleaseIdentifiers?.Any() == true)
        {
            if (this.Major == 0 && this.Minor == 0 && this.Patch == other.Patch)
            {
                if (this.PrereleaseIdentifiers?.Any() == true)
                {
                    return this.ComparePrereleases(other.PrereleaseIdentifiers!) >= 0;
                }
                return true;
            }
            return false;
        }
        return this.Major == 0 && this.Minor == 0 && this.Patch == other.Patch;
    }

    public int CompareTo(SemanticVersion? other)
    {
        if (other is null)
        {
            return 1;
        }
        
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

        return this.CompareTo(other) == 0;
    }

    private int ComparePrereleases(IReadOnlyList<PrereleaseIdentifier> otherList)
    {
        var thisList = this.PrereleaseIdentifiers!;

        foreach (var (a, b) in thisList.Zip(otherList, (a, b) => (a, b)))
        {
            var cmp = a.CompareTo(b);
            if (cmp != 0)
            {
                return cmp;
            }
        }

        return thisList.Count.CompareTo(otherList.Count);
    }
    
    private static IReadOnlyList<PrereleaseIdentifier> ParsePrerelease(string? prerelease)
    {
        if (string.IsNullOrEmpty(prerelease))
        {
            return [];
        }

        var prereleaseIdentifiers = prerelease.Split('.')
            .Select(x => new PrereleaseIdentifier(x))
            .ToList()
            .AsReadOnly();

        return prereleaseIdentifiers;
    }
    
    private static IReadOnlyList<BuildIdentifier> ParseBuildMetadata(string? build)
    {
        if (string.IsNullOrEmpty(build))
        {
            return [];
        }

        var buildIdentifiers = build.Split('.')
            .Select(x => new BuildIdentifier(x))
            .ToList()
            .AsReadOnly();

        return buildIdentifiers;
    }

    [GeneratedRegex(@"^\s*(?<major>0|[1-9]\d*)(?:\.(?<minor>0|[1-9]\d*))?(?:\.(?<patch>0|[1-9]\d*))?(?:-(?<prerelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+(?<build>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?\s*$")]
    private static partial Regex SemverRegex();
}