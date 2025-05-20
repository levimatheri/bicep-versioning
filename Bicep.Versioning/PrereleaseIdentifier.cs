namespace Bicep.Versioning;

public class PrereleaseIdentifier : IComparable<PrereleaseIdentifier>
{
    public string Value { get; }

    public PrereleaseIdentifier(string value)
    {
        Value = value;
    }

    public int CompareTo(PrereleaseIdentifier? other)
    {
        if (other is null)
        {
            // Per SemVer, a prerelease identifier has lower precedence than none.
            return 1;
        }

        bool thisIsNumeric = int.TryParse(Value, out int thisNum);
        bool otherIsNumeric = int.TryParse(other.Value, out int otherNum);

        if (thisIsNumeric && otherIsNumeric)
        {
            return thisNum.CompareTo(otherNum);
        }
        else if (thisIsNumeric)
        {
            // Numeric identifiers have lower precedence than non-numeric.
            return -1;
        }
        else if (otherIsNumeric)
        {
            return 1;
        }
        else
        {
            return string.Compare(Value, other.Value, StringComparison.Ordinal);
        }
    }
}