// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Scene.Model.Guards;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_SceneGuards;

// Intentional calls to obsolete members verify source/binary compatibility for existing consumers.
#pragma warning disable CS0618
public class when_preserving_the_legacy_api : Specification
{
    static readonly SourceLocation _at = SourceLocation.Start;
    static readonly ComparisonConditionSyntax _comparison = new("item.status", ComparisonOperator.Equal, new LiteralExpressionSyntax("open", _at), _at);
    static readonly ComparisonConditionSyntax _count = new("item.count", ComparisonOperator.GreaterThan, new LiteralExpressionSyntax(3, _at), _at);

    // All expected JSON below was emitted by origin/main (348e72fd)'s real SceneGuards implementation,
    // exported with git show and run in .ai-work/train8/shim/main; see main-shapes.log.
    const string ComparisonJson = """{"kind":"comparison","left":{"subject":"item.status"},"operator":"Equal","right":"open"}""";
    const string CountJson = """{"kind":"comparison","left":{"subject":"item.count"},"operator":"GreaterThan","right":3}""";

    [Fact] void should_keep_the_guard_path() => SceneGuards.GuardPath.ShouldEqual("$guard");
    [Fact] void should_keep_the_subject_key() => SceneGuards.SubjectKey.ShouldEqual("subject");
    [Fact] void should_keep_the_one_argument_conversion() => JsonSerializer.Serialize(SceneGuards.Convert(_comparison)).ShouldEqual(ComparisonJson);
    [Fact] void should_keep_the_explicit_null_resolver() => JsonSerializer.Serialize(SceneGuards.Convert(_comparison, null)).ShouldEqual(ComparisonJson);
    [Fact] void should_keep_the_subject_shape() => JsonSerializer.Serialize(SceneGuards.Subject("item.status")).ShouldEqual("""{"subject":"item.status"}""");

    [Fact]
    void should_keep_declared_enumeration_members() =>
        JsonSerializer.Serialize(SceneGuards.Convert(_comparison, path => path == "item.status" ? ["open", "closed"] : null))
            .ShouldEqual("""{"kind":"comparison","left":{"subject":"item.status"},"operator":"Equal","right":"open","enumeration":["open","closed"]}""");

    [Theory]
    [InlineData(LogicalOperator.And, "And")]
    [InlineData(LogicalOperator.Or, "Or")]
    void should_keep_logical_conditions(LogicalOperator operation, string name) =>
        JsonSerializer.Serialize(SceneGuards.Convert(new LogicalConditionSyntax(_comparison, operation, _count, _at)))
            .ShouldEqual("{\"kind\":\"logical\",\"left\":" + ComparisonJson + ",\"operator\":\"" + name + "\",\"right\":" + CountJson + "}");

    [Fact]
    void should_keep_the_unsupported_condition_kind() =>
        JsonSerializer.Serialize(SceneGuards.Convert(new NotConditionSyntax(_comparison, _at))).ShouldEqual("""{"kind":"NotConditionSyntax"}""");

    [Fact]
    void should_keep_an_unknown_comparison_operator() =>
        JsonSerializer.Serialize(SceneGuards.Convert(_comparison with { Operator = (ComparisonOperator)123 }))
            .ShouldEqual("""{"kind":"comparison","left":{"subject":"item.status"},"operator":"123","right":"open"}""");

    [Fact]
    void should_keep_an_unknown_logical_operator_and_nested_unsupported_condition() =>
        JsonSerializer.Serialize(SceneGuards.Convert(new LogicalConditionSyntax(_comparison, (LogicalOperator)123, new NotConditionSyntax(_comparison, _at), _at)))
            .ShouldEqual("{\"kind\":\"logical\",\"left\":" + ComparisonJson + ",\"operator\":\"123\",\"right\":{\"kind\":\"NotConditionSyntax\"}}");

    [Fact]
    void should_keep_the_unsupported_expression_kind() =>
        JsonSerializer.Serialize(SceneGuards.Convert(_comparison with { Right = new PathExpressionSyntax("item.other", _at) }))
            .ShouldEqual("""{"kind":"comparison","left":{"subject":"item.status"},"operator":"Equal","right":{"kind":"PathExpressionSyntax"}}""");

    [Fact]
    void should_keep_a_null_literal() =>
        JsonSerializer.Serialize(SceneGuards.Convert(_comparison with { Right = new LiteralExpressionSyntax(null, _at) }))
            .ShouldEqual("""{"kind":"comparison","left":{"subject":"item.status"},"operator":"Equal","right":null}""");

    [Fact]
    void should_leave_the_typed_unsupported_condition_unchanged() =>
        SceneGuards.ConvertToCondition(new NotConditionSyntax(_comparison, _at))
            .ShouldEqual(new ComparisonGuardCondition(new SubjectReference(string.Empty), GuardOperator.Equal, null));

    [Fact]
    void should_leave_the_typed_null_condition_unchanged() =>
        SceneGuards.ConvertToCondition(null!)
            .ShouldEqual(new ComparisonGuardCondition(new SubjectReference(string.Empty), GuardOperator.Equal, null));

    [Theory]
    [InlineData(-1)]
    [InlineData(123)]
    void should_leave_the_typed_unknown_operator_unchanged(int operation) =>
        SceneGuards.ConvertToCondition(_comparison with { Operator = (ComparisonOperator)operation })
            .ShouldEqual(new ComparisonGuardCondition(new SubjectReference("item.status"), (GuardOperator)operation, "open"));

    // Screenplay has no built-in not node; this extension pins the unsupported-condition contract.
    sealed record NotConditionSyntax(ConditionSyntax Condition, SourceLocation Location) : ConditionSyntax(Location);
}
#pragma warning restore CS0618
