namespace Bicep.Versioning;

public class PrereleaseIdentifier
{
    public string Value { get; }

    public PrereleaseIdentifier(string value)
    {
        Value = value;
    }
}