// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Scene.Model.Guards;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_SceneGuards;

public class when_admitting_contains : Specification
{
    [Theory]
    [InlineData("1")]
    [InlineData("true")]
    [InlineData("null")]
    [InlineData("[1]")]
    [InlineData("{}")]
    void should_refuse_non_string_literals_in_serialized_scene(string literal) =>
        SceneGuards.CanEvaluate(JsonNode.Parse("{\"kind\":\"comparison\",\"left\":{\"subject\":\"item.value\"},\"operator\":\"Contains\",\"right\":" + literal + "}")).ShouldBeFalse();

    [Theory]
    [InlineData(1)]
    [InlineData(true)]
    [InlineData(null)]
    void should_preserve_refusal_through_typed_conversion(object? literal)
    {
        var at = SourceLocation.Start;
        var condition = SceneGuards.ConvertToCondition(new ComparisonConditionSyntax("item.value", ComparisonOperator.Contains, new LiteralExpressionSyntax(literal, at), at));
        SceneGuards.CanEvaluate(condition).ShouldBeFalse();
    }

    [Fact]
    void should_admit_a_string_literal() =>
        SceneGuards.CanEvaluate(new ComparisonGuardCondition(new SubjectReference("item.value"), GuardOperator.Contains, "open")).ShouldBeTrue();
}
