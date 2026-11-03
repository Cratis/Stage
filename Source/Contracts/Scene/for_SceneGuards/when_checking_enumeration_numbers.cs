// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Scene.Engine.Guards;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_SceneGuards;

public class when_checking_enumeration_numbers : Specification
{
    static JsonObject Comparison(JsonNode? numbers) => new()
    {
        ["kind"] = "comparison",
        ["left"] = new JsonObject { ["subject"] = "item.status" },
        ["operator"] = "Equal",
        ["right"] = "open",
        ["enumeration"] = new JsonArray("open", "closed"),
        ["enumerationNumbers"] = numbers
    };

    static bool CanEvaluate(JsonObject condition) => GuardConditions.CanEvaluate(JsonSerializer.SerializeToElement(condition));
    static bool? Evaluate(JsonObject condition, int status) => GuardConditions.Evaluate(JsonSerializer.SerializeToElement(condition), JsonSerializer.SerializeToElement(new { status }));

    [Fact] void should_admit_reordered_numbers() => CanEvaluate(Comparison(new JsonArray(1, 0))).ShouldBeTrue();
    [Fact] void should_admit_sparse_negative_numbers() => CanEvaluate(Comparison(new JsonArray(-1, 90))).ShouldBeTrue();
    [Fact] void should_refuse_null_numbers() => CanEvaluate(Comparison(null)).ShouldBeFalse();
    [Fact] void should_refuse_duplicate_numbers() => CanEvaluate(Comparison(new JsonArray(1, 1))).ShouldBeFalse();
    [Fact] void should_refuse_mismatched_numbers() => CanEvaluate(Comparison(new JsonArray(1))).ShouldBeFalse();
    [Fact] void should_refuse_fractional_numbers() => CanEvaluate(Comparison(new JsonArray(1.5, 0))).ShouldBeFalse();
    [Fact] void should_refuse_text_numbers() => CanEvaluate(Comparison(new JsonArray("1", "0"))).ShouldBeFalse();
    [Fact] void should_refuse_non_array_numbers() => CanEvaluate(Comparison(JsonValue.Create(1))).ShouldBeFalse();

    [Fact]
    void should_refuse_numbers_without_members()
    {
        var condition = Comparison(new JsonArray(1, 0));
        condition.Remove("enumeration");
        CanEvaluate(condition).ShouldBeFalse();
    }

    [Fact]
    void should_admit_absent_numbers()
    {
        var condition = Comparison(new JsonArray(1, 0));
        condition.Remove("enumerationNumbers");
        CanEvaluate(condition).ShouldBeTrue();
    }

    [Fact]
    void should_admit_an_authored_explicit_number_outside_the_ordinal_range()
    {
        var condition = Comparison(new JsonArray(-1, 90));
        condition["right"] = 90;
        SceneGuards.CanEvaluate(condition).ShouldBeTrue();
        SceneGuards.EnumerationMembershipDiagnostic(condition).ShouldBeNull();
    }

    [Fact]
    void should_diagnose_an_ordinal_that_is_not_an_explicit_member_number()
    {
        var condition = Comparison(new JsonArray(-1, 90));
        condition["right"] = 0;
        SceneGuards.CanEvaluate(condition).ShouldBeFalse();
        SceneGuards.EnumerationMembershipDiagnostic(condition).ShouldEqual(SceneGuards.UnknownEnumerationLiteralDiagnosticCode);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(7, false)]
    void should_use_explicit_identity_instead_of_declaration_position(int status, bool expected) =>
        Evaluate(Comparison(new JsonArray(1, 0)), status).ShouldEqual(expected);

    [Fact]
    void should_refuse_an_unknown_number_even_for_not_equal()
    {
        var condition = Comparison(new JsonArray(1, 0));
        condition["operator"] = "NotEqual";
        Evaluate(condition, 7).ShouldEqual(false);
    }

    [Fact]
    void should_refuse_a_logical_condition_with_malformed_numbers()
    {
        var logical = new JsonObject
        {
            ["kind"] = "logical",
            ["operator"] = "Or",
            ["left"] = Comparison(new JsonArray(1, 1)),
            ["right"] = Comparison(new JsonArray(1, 0))
        };
        CanEvaluate(logical).ShouldBeFalse();
    }
}
