using Sprache;

namespace Bicep.Versioning.Sprache;

public class SemVerRangeParser
{
    static readonly Parser<string> Operator =
        Parse.String(">=").Text()
        .Or(Parse.String("<=").Text())
        .Or(Parse.String(">").Text())
        .Or(Parse.String("<").Text())
        .Or(Parse.String("~").Text())
        .Or(Parse.String("^").Text())
        .Or(Parse.String("=").Text());

    static readonly Parser<int> Number =
        from digits in Parse.Number
        select int.Parse(digits);

    static readonly Parser<string[]> Prerelease =
        from dash in Parse.Char('-')
        from ids in Parse.LetterOrDigit.Or(Parse.Char('-')).AtLeastOnce().Text()
            .DelimitedBy(Parse.Char('.'))
        select ids.ToArray();

    static readonly Parser<string[]> Build =
        from plus in Parse.Char('+')
        from ids in Parse.LetterOrDigit.Or(Parse.Char('-')).AtLeastOnce().Text()
            .DelimitedBy(Parse.Char('.'))
        select ids.ToArray();

    static readonly Parser<SemVerVersion> Version =
        from major in Number
        from dot1 in Parse.Char('.')
        from minor in Number
        from dot2 in Parse.Char('.')
        from patch in Number
        from prerelease in Prerelease.Optional()
        from build in Build.Optional()
        select new SemVerVersion(
            major,
            minor,
            patch,
            prerelease.IsDefined ? prerelease.Get() : [],
            build.IsDefined ? build.Get() : []
        );

    public static readonly Parser<SemVerRange> Range =
        from op in Operator.Token()
        from version in Version
        select new SemVerRange { Operator = op, Version = version };

    public static readonly Parser<SemVerVersion> SemVer =
        from major in Number
        from dot1 in Parse.Char('.')
        from minor in Number
        from dot2 in Parse.Char('.')
        from patch in Number
        from prerelease in Prerelease.Optional()
        from build in Build.Optional()
        select new SemVerVersion(
            major,
            minor,
            patch,
            prerelease.IsDefined ? prerelease.Get() : [],
            build.IsDefined ? build.Get() : []
        );
}

public class SemVerRange
{
    public string Operator { get; set; }
    public SemVerVersion Version { get; set; }

    public override string ToString() => $"{Operator}{Version}";

    public bool IsSatisfiedBy(SemVerVersion other)
    {
        int cmp = other.CompareTo(Version);
        return Operator switch
        {
            ">=" => cmp >= 0,
            "<=" => cmp <= 0,
            ">" => cmp > 0,
            "<" => cmp < 0,
            "~" => other.IsCompatibleWithTilde(Version),
            "^" => other.IsCompatibleWithCaret(Version),
            "=" => cmp == 0,
            "" => cmp == 0,
            null => cmp == 0,
            _ => false
        };
    }
}

public class SemVerVersion : IComparable<SemVerVersion>
{
    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }
    public string[] Prerelease { get; }
    public string[] Build { get; }

    public SemVerVersion(int major, int minor, int patch, string[] prerelease, string[] build)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        Prerelease = prerelease;
        Build = build;
    }

    public override string ToString()
    {
        var version = $"{Major}.{Minor}.{Patch}";
        if (Prerelease.Length > 0)
            version += "-" + string.Join('.', Prerelease);
        if (Build.Length > 0)
            version += "+" + string.Join('.', Build);
        return version;
    }

    public int CompareTo(SemVerVersion? other)
    {
        if (other is null) return 1;
        int cmp = Major.CompareTo(other.Major);
        if (cmp != 0) return cmp;
        cmp = Minor.CompareTo(other.Minor);
        if (cmp != 0) return cmp;
        cmp = Patch.CompareTo(other.Patch);
        if (cmp != 0) return cmp;
        // Prerelease comparison: empty prerelease is higher precedence
        if (Prerelease.Length == 0 && other.Prerelease.Length > 0) return 1;
        if (Prerelease.Length > 0 && other.Prerelease.Length == 0) return -1;
        for (int i = 0; i < Math.Min(Prerelease.Length, other.Prerelease.Length); i++)
        {
            var a = Prerelease[i];
            var b = other.Prerelease[i];
            bool aNum = int.TryParse(a, out int aInt);
            bool bNum = int.TryParse(b, out int bInt);
            if (aNum && bNum)
            {
                cmp = aInt.CompareTo(bInt);
                if (cmp != 0) return cmp;
            }
            else if (aNum)
            {
                return -1;
            }
            else if (bNum)
            {
                return 1;
            }
            else
            {
                cmp = string.CompareOrdinal(a, b);
                if (cmp != 0) return cmp;
            }
        }
        return Prerelease.Length.CompareTo(other.Prerelease.Length);
    }

        public bool IsCompatibleWithTilde(SemVerVersion range)
        {
            // ~1.2.3 := >=1.2.3 <1.3.0 (including prerelease/build if present in range)
            if (Major != range.Major) return false;
            if (Minor != range.Minor) return false;

            // Lower bound: >= range (including prerelease/build)
            if (CompareTo(range) < 0) return false;

            // Upper bound: < next minor version
            var upper = new SemVerVersion(Major, Minor + 1, 0, [], []);
            if (CompareTo(upper) >= 0) return false;

            // If range has prerelease, only prerelease versions with same identifiers are allowed
            if (range.Prerelease.Length > 0)
            {
                if (Prerelease.Length == 0) return false;
                for (int i = 0; i < range.Prerelease.Length; i++)
                {
                    if (i >= Prerelease.Length || Prerelease[i] != range.Prerelease[i])
                        return false;
                }
            }

            // Build metadata is not considered for range satisfaction
            return true;
        }

    public bool IsCompatibleWithCaret(SemVerVersion range)
    {
        // ^1.2.3 := >=1.2.3 <2.0.0
        // ^0.2.3 := >=0.2.3 <0.3.0
        // ^0.0.3 := >=0.0.3 <0.0.4
        if (CompareTo(range) < 0) return false;

        SemVerVersion upper;
        if (range.Major > 0)
        {
            upper = new SemVerVersion(range.Major + 1, 0, 0, [], []);
        }
        else if (range.Minor > 0)
        {
            upper = new SemVerVersion(0, range.Minor + 1, 0, [], []);
        }
        else
        {
            upper = new SemVerVersion(0, 0, range.Patch + 1, [], []);
        }
        // Use CompareTo for upper bound, but allow prerelease versions if range is a prerelease
        if (Major != upper.Major || Minor != upper.Minor || Patch != upper.Patch)
        {
            if (CompareTo(upper) >= 0) return false;
        }
        else
        {
            // If upper bound is exactly the next version, do not allow it
            if (CompareTo(upper) >= 0) return false;
        }

        // If range has prerelease, only prerelease versions with same identifiers are allowed
        if (range.Prerelease.Length > 0)
        {
            if (Prerelease.Length == 0) return false;
            for (int i = 0; i < range.Prerelease.Length; i++)
            {
                if (i >= Prerelease.Length || Prerelease[i] != range.Prerelease[i])
                    return false;
            }
        }

        // Build metadata is not considered for range satisfaction
        return true;
    }
}