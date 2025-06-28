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

        var thisIsNumeric = int.TryParse(Value, out var thisNum);
        var otherIsNumeric = int.TryParse(other.Value, out var otherNum);

        return thisIsNumeric switch
        {
            true when otherIsNumeric => thisNum.CompareTo(otherNum),
            true => -1, // Numeric identifiers have lower precedence than non-numeric.
            _ => otherIsNumeric ? 1 : string.Compare(Value, other.Value, StringComparison.Ordinal)
        };
    }
}