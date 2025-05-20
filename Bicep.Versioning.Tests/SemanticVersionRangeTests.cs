using FluentAssertions;

namespace Bicep.Versioning.Tests;

[TestClass]
public class SemanticVersionRangeTests
{
    [TestMethod]
    [DynamicData(nameof(GetValidVersionRangeBasic))]
    public void ParseVersionRange_ValidRange_ReturnsExpectedProperties(
        string raw, IList<TestSemanticVersionRange> expected)
    {
        TestValidVersion(raw, expected);
    }

    private void TestValidVersion(string versionRangeRaw, IList<TestSemanticVersionRange> expected)
    {
        var versionRange = SemanticVersionRange.Parse(versionRangeRaw);
        versionRange.Should().HaveCountGreaterThan(0);
        versionRange.Should().HaveCount(expected.Count());
        for (int i = 0; i < versionRange.Count; i++)
        {
            var versionRangeItem = versionRange[i];
            var expectedItem = expected[i];
            versionRangeItem.Operation.Operator.Should().Be(expectedItem.ConstraintOperator);
            versionRangeItem.Version.ToString().Should().Be(expectedItem.Version);
        }
    }

    private static IEnumerable<object[]> GetValidVersionRangeBasic =>
        [
            [
                ">= 1.2.3",
                new[]
                {
                    new TestSemanticVersionRange { ConstraintOperator = ConstraintOperator.GreaterThanOrEqual, Version = "1.2.3" }
                }
            ],
            [
                "<= 2.3.4",
                new[]
                {
                    new TestSemanticVersionRange { ConstraintOperator = ConstraintOperator.LessThanOrEqual, Version = "2.3.4" }
                }
            ],
            [
                "1.2.3",
                new[]
                {
                    new TestSemanticVersionRange { ConstraintOperator = ConstraintOperator.Equal, Version = "1.2.3" }
                }
            ],
            [
                ">= 1.2.3, < 2.0.0",
                new[]
                {
                    new TestSemanticVersionRange { ConstraintOperator = ConstraintOperator.GreaterThanOrEqual, Version = "1.2.3" },
                    new TestSemanticVersionRange { ConstraintOperator = ConstraintOperator.LessThan, Version = "2.0.0" }
                }
            ],
            [
                "~1.2.3",
                new[]
                {
                    new TestSemanticVersionRange { ConstraintOperator = ConstraintOperator.Tilde, Version = "1.2.3" }
                }
            ],
            [
                "^1.2.3",
                new[]
                {
                    new TestSemanticVersionRange { ConstraintOperator = ConstraintOperator.Caret, Version = "1.2.3" }
                }
            ],
        ];

    public class TestSemanticVersionRange
    {
        public required ConstraintOperator ConstraintOperator { get; set; }
        public required string Version { get; set; }
    }
}
