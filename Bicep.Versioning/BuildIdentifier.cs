namespace Bicep.Versioning;

public class BuildIdentifier
{
    public string Value { get; }

    public BuildIdentifier(string value)
    {
        Value = value;
    }
}