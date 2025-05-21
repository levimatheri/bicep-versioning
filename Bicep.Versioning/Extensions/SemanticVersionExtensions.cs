using System;

namespace Bicep.Versioning.Extensions;

public static class SemanticVersionExtensions
{
    public static bool Satisfies(this SemanticVersion version, IEnumerable<SemanticVersionRange> ranges)
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
