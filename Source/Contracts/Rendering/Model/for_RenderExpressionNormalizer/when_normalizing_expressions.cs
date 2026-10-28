// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.Model.for_RenderExpressionNormalizer;

public class when_normalizing_expressions : for_RenderModelBuilder.given.a_response_only_command
{
    RenderExpressionNormalizer _normalizer = null!;

    void Establish() => _normalizer = new(new(_request.Model.Application));

    [Fact]
    void should_resolve_property_types()
    {
        var expression = new SemanticResolvedExpression(SemanticExpressionRootKind.Command, SemanticExpressionSourceKind.Property, _command.Properties[0].Id);
        var result = Assert.IsType<RenderExpression.Resolved>(_normalizer.Normalize(expression));
        result.Target.ShouldEqual(expression.Target);
        result.TypeState.ShouldEqual(RenderTypeState.Resolved);
        result.Type!.Primitive.ShouldEqual(SemanticPrimitiveType.Text);
    }

    [Fact]
    void should_resolve_read_path_types()
    {
        var result = Assert.IsType<RenderExpression.ReadPath>(_normalizer.Normalize(new SemanticReadPathExpression("current", _command.Properties[0].Id)));
        result.Read.ShouldEqual("current");
        result.TypeState.ShouldEqual(RenderTypeState.Resolved);
    }

    [Fact]
    void should_preserve_refusal_member_and_type()
    {
        var result = Assert.IsType<RenderExpression.Refusal>(_normalizer.Normalize(new SemanticRefusalExpression("message", _command.Properties[0].Type)));
        result.Member.ShouldEqual("message");
        result.TypeState.ShouldEqual(RenderTypeState.Resolved);
    }

    [Fact]
    void should_preserve_event_context_type() => Assert.IsType<RenderExpression.EventContext>(_normalizer.Normalize(new SemanticEventContextExpression(SemanticEventContextValueKind.EventSourceIdentity, _command.Properties[0].Type))).ContextValue.ShouldEqual(SemanticEventContextValueKind.EventSourceIdentity);

    [Fact]
    void should_mark_untyped_literals_explicitly() => _normalizer.Normalize(new SemanticValueExpression(new SemanticTextValue("hello"))).TypeState.ShouldEqual(RenderTypeState.AbsentContextualType);

    [Fact]
    void should_not_guess_a_source_span() => _normalizer.Normalize(new SemanticValueExpression(new SemanticNullValue())).Provenance.Status.ShouldEqual("Absent");

    [Theory]
    [InlineData(SemanticComparisonOperator.Equal)]
    [InlineData(SemanticComparisonOperator.NotEqual)]
    [InlineData(SemanticComparisonOperator.LessThan)]
    [InlineData(SemanticComparisonOperator.LessThanOrEqual)]
    [InlineData(SemanticComparisonOperator.GreaterThan)]
    [InlineData(SemanticComparisonOperator.GreaterThanOrEqual)]
    void should_preserve_comparison_operators(SemanticComparisonOperator operation)
    {
        var result = Assert.IsType<RenderExpression.Comparison>(_normalizer.Condition(new SemanticComparison(new(default, new SemanticNumberValue(1)), operation, new(default, new SemanticNumberValue(2)))));
        result.Operator.ShouldEqual(operation);
        result.Type!.Primitive.ShouldEqual(SemanticPrimitiveType.Boolean);
    }

    [Theory]
    [InlineData(SemanticLogicalOperator.And)]
    [InlineData(SemanticLogicalOperator.Or)]
    void should_preserve_logical_operators(SemanticLogicalOperator operation)
    {
        var condition = new SemanticComparison(new(default, new SemanticBooleanValue(true)), SemanticComparisonOperator.Equal, new(default, new SemanticBooleanValue(false)));
        Assert.IsType<RenderExpression.Logical>(_normalizer.Condition(new SemanticLogicalCondition(condition, operation, condition))).Operator.ShouldEqual(operation);
    }

    [Fact]
    void should_preserve_nested_literals()
    {
        var value = new SemanticCompositeValue([new(_command.Properties[0].Id, new SemanticArrayValue([new SemanticNullValue(), new SemanticTextValue("text"), new SemanticNumberValue(3), new SemanticBooleanValue(true)]))]);
        var result = Assert.IsType<RenderExpression.Value>(_normalizer.Normalize(new SemanticValueExpression(value)));
        var values = Assert.IsType<RenderLiteral.Array>(Assert.IsType<RenderLiteral.Composite>(result.Literal).Properties.Single().Value).Values;
        values.ShouldEqual([new RenderLiteral.Null(), new RenderLiteral.Text("text"), new RenderLiteral.Number(3), new RenderLiteral.Boolean(true)]);
    }

    [Fact]
    void should_reject_an_unknown_expression() => Assert.Throws<InvalidArtifactRenderContract>(() => _normalizer.Normalize(new UnknownExpression()));

    [Fact]
    void should_reject_an_unknown_comparison() => Assert.Throws<InvalidArtifactRenderContract>(() => _normalizer.Condition(new SemanticComparison(new(default, new SemanticNumberValue(1)), (SemanticComparisonOperator)999, new(default, new SemanticNumberValue(2)))));

    sealed record UnknownExpression() : SemanticExpression(SemanticExpressionKind.Unknown);
}
