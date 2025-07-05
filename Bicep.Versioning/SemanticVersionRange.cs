using System.Text.RegularExpressions;

namespace Bicep.Versioning;

public partial class SemanticVersionRange
{
    private SemanticVersionRange(ConstraintOperation operation,  SemanticVersion version)
    {
        Operation = operation;
        Version = version;
    }
    
    public static IReadOnlyList<SemanticVersionRange> Parse(string range)
    {
        var ranges = range.Split([','], StringSplitOptions.RemoveEmptyEntries);
        var result = new List<SemanticVersionRange>();
        foreach (var r in ranges)
        {
            var trimmed = r.Trim();
            var match = ConstraintRegex().Match(trimmed);
            if (!match.Success)
            {
                throw new FormatException($"Malformed version range: {trimmed}");
            }
            var opRaw = match.Groups["operator"];
            var versionRaw = match.Groups["version"];
            var opString = opRaw.Success ? opRaw.Value.Trim() : "";
            var version = SemanticVersion.Parse(versionRaw.Value);

            switch (opString)
            {
                case "~":
                {
                    result.AddRange(ParseTilde(version));
                    break;
                }
                case "^":
                {
                    result.AddRange(ParseCaret(version));
                    break;
                }
                default:
                    result.Add(ParseSingle(opString, version));
                    break;
            }
        }
        return result;
    }
    
    private static SemanticVersionRange ParseSingle(string opString, SemanticVersion version)
    {
        if (!ConstraintOperations.TryGetValue(opString, out var operation))
        {
            throw new FormatException($"Unknown operator: {opString}");
        }
        return new SemanticVersionRange(operation, version);
    }

    private static List<SemanticVersionRange> ParseTilde(SemanticVersion version)
    {
        var result = new List<SemanticVersionRange>();
        var upperBound = new SemanticVersion(version.Major, version.Minor + 1, 0);
        if (version.OmittedComponent.HasFlag(OmittedComponent.Minor))
        {
            upperBound = new SemanticVersion(version.Major + 1, 0, 0);
        }
        
        result.Add(new SemanticVersionRange(ConstraintOperations[">="], version));
        result.Add(new SemanticVersionRange(ConstraintOperations["<"], upperBound));
        
        return result;
    }
    
    private static List<SemanticVersionRange> ParseCaret(SemanticVersion version)
    {
        var result = new List<SemanticVersionRange>();
        SemanticVersion upperBound;
        if (version.Major > 0)
        {
            upperBound = new SemanticVersion(version.Major + 1, 0, 0);
        }
        else if (version.Minor > 0)
        {
            upperBound = new SemanticVersion(0, version.Minor + 1, 0);
        }
        else
        {
            upperBound = new SemanticVersion(0, 0, version.Patch + 1);
        }
        
        result.Add(new SemanticVersionRange(ConstraintOperations[">="], version));
        result.Add(new SemanticVersionRange(ConstraintOperations["<"], upperBound));
        
        return result;
    }

    public ConstraintOperation Operation { get; }
    public SemanticVersion Version { get; }


    private static readonly IReadOnlyDictionary<string, ConstraintOperation> ConstraintOperations = new Dictionary<string, ConstraintOperation>
    {
        { "", new ConstraintOperation(ConstraintOperator.Equal, (a, b) => a.Equals(b)) },
        { "=", new ConstraintOperation(ConstraintOperator.Equal, (a, b) => a.Equals(b)) },
        { ">", new ConstraintOperation(ConstraintOperator.GreaterThan, (a, b) => a.GreaterThan(b)) },
        { "<", new ConstraintOperation(ConstraintOperator.LessThan, (a, b) => a.LessThan(b)) },
        { ">=", new ConstraintOperation(ConstraintOperator.GreaterThanOrEqual, (a, b) => a.GreaterThanOrEqual(b)) },
        { "<=", new ConstraintOperation(ConstraintOperator.LessThanOrEqual, (a, b) => a.LessThanOrEqual(b)) },
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
    LessThanOrEqual
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