using System.Text.RegularExpressions;

namespace Bicep.Versioning;

public partial class SemanticVersionRange
{
    private SemanticVersionRange(ConstraintOperation operation,  SemanticVersion version, string raw)
    {
        Operation = operation;
        Version = version;
        Raw = raw;
    }
    
    public static IReadOnlyList<SemanticVersionRange> Parse(string range)
    {
        var ranges = range.Split([','], StringSplitOptions.RemoveEmptyEntries);
        var parsedRanges = new List<SemanticVersionRange>();

        foreach (var r in ranges)
        {
            parsedRanges.Add(ParseSingle(r));
        }

        return parsedRanges;
    }

    private static SemanticVersionRange ParseSingle(string range)
    {
        var match = ConstraintRegex().Match(range);
        if (!match.Success)
        {
            throw new FormatException($"Malformed version range: {range}");
        }

        var opRaw = match.Groups["operator"];
        var versionRaw = match.Groups["version"];
        var opString = opRaw.Success ? opRaw.Value.Trim() : "";
        if (!ConstraintOperations.TryGetValue(opString, out var operation))
        {
            throw new FormatException($"Unknown operator: {opString}");
        }
        var version = SemanticVersion.Parse(versionRaw.Value);
        return new SemanticVersionRange(operation, version, range);
    }


    public ConstraintOperation Operation { get; }
    public SemanticVersion Version { get; }
    public string Raw { get; }

    

    private static IReadOnlyDictionary<string, ConstraintOperation> ConstraintOperations = new Dictionary<string, ConstraintOperation>
    {
        { "", new ConstraintOperation(ConstraintOperator.Equal, (v1, v2) => v1.Equals(v2)) },
        { "=", new ConstraintOperation(ConstraintOperator.Equal, (v1, v2) => v1.Equals(v2)) },
        { ">", new ConstraintOperation(ConstraintOperator.GreaterThan, (v1, v2) => v1.GreaterThan(v2)) },
        { "<", new ConstraintOperation(ConstraintOperator.LessThan, (v1, v2) => v1.LessThan(v2)) },
        { ">=", new ConstraintOperation(ConstraintOperator.GreaterThanOrEqual, (v1, v2) => v1.GreaterThanOrEqual(v2)) },
        { "<=", new ConstraintOperation(ConstraintOperator.LessThanOrEqual, (v1, v2) => v1.LessThanOrEqual(v2)) },
        { "^", new ConstraintOperation(ConstraintOperator.Caret, (v1, v2) => v1.IsCaretSatisfied(v2)) },
        { "~", new ConstraintOperation(ConstraintOperator.Tilde, (v1, v2) => v1.IsTildeSatisfied(v2)) }
    };

    // TODO: Revisit this regex
    [GeneratedRegex(@"^\s*(?:(?<operator>>=|<=|~>|!=|=|>|<|\^|~)\s*)?(?<version>\d+\.\d+\.\d+(?:-[\w\.-]+)?(?:\+[\w\.-]+)?)\s*$")]
    private static partial Regex ConstraintRegex();
}

public enum ConstraintOperator
{
    None, // TODO: is this needed?
    Equal,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
    Caret,
    Tilde
}

public class ConstraintOperation
{
    public ConstraintOperator Operator { get; }
    public Func<SemanticVersion, SemanticVersion, bool> Operation { get; }
    public ConstraintOperation(ConstraintOperator @operator, Func<SemanticVersion, SemanticVersion, bool> operation)
    {
        Operator = @operator;
        Operation = operation;
    }
}