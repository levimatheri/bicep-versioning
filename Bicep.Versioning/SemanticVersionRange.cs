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

        return ranges.Select(ParseSingle).ToList();
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


    private static readonly IReadOnlyDictionary<string, ConstraintOperation> ConstraintOperations = new Dictionary<string, ConstraintOperation>
    {
        { "", new ConstraintOperation(ConstraintOperator.Equal, (a, b) => a.Equals(b)) },
        { "=", new ConstraintOperation(ConstraintOperator.Equal, (a, b) => a.Equals(b)) },
        { ">", new ConstraintOperation(ConstraintOperator.GreaterThan, (a, b) => a.GreaterThan(b)) },
        { "<", new ConstraintOperation(ConstraintOperator.LessThan, (a, b) => a.LessThan(b)) },
        { ">=", new ConstraintOperation(ConstraintOperator.GreaterThanOrEqual, (a, b) => a.GreaterThanOrEqual(b)) },
        { "<=", new ConstraintOperation(ConstraintOperator.LessThanOrEqual, (a, b) => a.LessThanOrEqual(b)) },
        { "^", new ConstraintOperation(ConstraintOperator.Caret, (a, b) => a.SatisfiesCaretRange(b)) },
        { "~", new ConstraintOperation(ConstraintOperator.Tilde, (a, b) => a.SatisfiesTildeRange(b)) }
    };

    [GeneratedRegex(@"^\s*(?:(?<operator>>=|<=|=|>|<|\^|~)\s*)?(?<version>.+?)\s*$")]
    private static partial Regex ConstraintRegex();
}

public enum ConstraintOperator
{
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
    public ConstraintOperation(ConstraintOperator @operator, Func<SemanticVersion, SemanticVersion, bool> operation)
    {
        Operator = @operator;
        Evaluator = operation;
    }

    public ConstraintOperator Operator { get; }
    public Func<SemanticVersion, SemanticVersion, bool> Evaluator { get; }
}