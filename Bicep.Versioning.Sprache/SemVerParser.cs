using System;
using System.Collections.Generic;
using Bicep.Versioning.Sprache;
using Sprache;


public static class SemVerParser
{
    private static readonly Parser<int> NumericIdentifier =
        from num in Parse.Number
        where num.Length == 1 || num[0] != '0'
        select int.Parse(num);

    private static readonly Parser<string> AlphanumIdentifier =
        Parse.LetterOrDigit.Or(Parse.Char('-')).AtLeastOnce().Text();

    private static readonly Parser<string> PreRelease =
        from dash in Parse.Char('-')
        from id in AlphanumIdentifier.DelimitedBy(Parse.Char('.')).Select(parts => string.Join(".", parts))
        select id;

    private static readonly Parser<string> BuildMetadata =
        from plus in Parse.Char('+')
        from id in AlphanumIdentifier.DelimitedBy(Parse.Char('.')).Select(parts => string.Join(".", parts))
        select id;

    public static readonly Parser<SemVerVersion> Parser =
        from major in NumericIdentifier
        from dot1 in Parse.Char('.')
        from minor in NumericIdentifier
        from dot2 in Parse.Char('.')
        from patch in NumericIdentifier
        from pre in PreRelease.Optional()
        from build in BuildMetadata.Optional()
        select new SemVerVersion(
            major,
            minor,
            patch,
            pre.IsDefined ? pre.Get().Split('.') : Array.Empty<string>(),
            build.IsDefined ? build.Get().Split('.') : Array.Empty<string>()
        );

    public static SemVerVersion ParseSemVer(string input)
    {
        return Parser.End().Parse(input);
    }
}
