namespace Bicep.Versioning.Extensions;

public static class SemanticVersionExtensions
{
    public static bool Satisfies(this SemanticVersion version, IEnumerable<SemanticVersionRange> ranges)
        => ranges.All(range => range.Operation.Evaluator.Invoke(version, range.Version));
}
