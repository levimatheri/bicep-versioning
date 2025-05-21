using System;

namespace Bicep.Versioning.Extensions;

public static class SemanticVersionRangeExtensions
{
    public static bool Satisfies(this IEnumerable<SemanticVersionRange> ranges, SemanticVersion version)
    {
        foreach (var range in ranges)
        {
            if (!range.Operation.Evaluator.Invoke(version, range.Version))
            {
                return false;
            }
        }

        return true;
    }
}
