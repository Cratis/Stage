// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Scene.Engine.Guards;
using Cratis.Scene.Model.Guards;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_SceneGuards;

public class when_checking_a_comparison_against_an_enumeration : Specification
{
    static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

    static Dictionary<string, object?> Comparison(object? right) => new()
    {
        ["kind"] = "comparison",
        ["left"] = SceneGuards.SubjectReferenceFor("item.status"),
        ["operator"] = "Equal",
        ["right"] = right,
        ["enumeration"] = new[] { "open", "closed" },
    };

    [Fact] void should_evaluate_a_member() => SceneGuards.CanEvaluate(Comparison("open")).ShouldBeTrue();
    [Fact] void should_evaluate_a_member_in_any_casing() => SceneGuards.CanEvaluate(Comparison("Closed")).ShouldBeTrue();
    [Fact] void should_refuse_a_literal_that_is_not_a_member() => SceneGuards.CanEvaluate(Comparison("archived")).ShouldBeFalse();
    [Fact] void should_report_the_distinct_stage_membership_diagnostic() => SceneGuards.EnumerationMembershipDiagnostic(Comparison("archived")).ShouldEqual("STAGE-SCENE-GUARD-001");
    [Fact] void should_not_report_a_membership_diagnostic_for_a_member() => SceneGuards.EnumerationMembershipDiagnostic(Comparison("OPEN")).ShouldBeNull();
    [Fact] void should_not_redefine_the_published_runtime_capability_predicate() => GuardConditions.CanEvaluate(JsonSerializer.SerializeToElement(Comparison("archived"), _options)).ShouldBeTrue();
    [Fact] void should_leave_unknown_member_execution_false_in_the_runtime() => GuardConditions.Evaluate(JsonSerializer.SerializeToElement(Comparison("archived"), _options), JsonSerializer.SerializeToElement(new { status = "open" }, _options)).ShouldEqual(false);
    [Fact] void should_preserve_case_insensitive_runtime_execution() => GuardConditions.Evaluate(JsonSerializer.SerializeToElement(Comparison("CLOSED"), _options), JsonSerializer.SerializeToElement(new { status = "closed" }, _options)).ShouldEqual(true);
    [Fact] void should_preserve_ordinal_runtime_execution() => GuardConditions.Evaluate(JsonSerializer.SerializeToElement(Comparison(1), _options), JsonSerializer.SerializeToElement(new { status = "closed" }, _options)).ShouldEqual(true);
    [Fact] void should_preserve_null_literal_capability() => SceneGuards.CanEvaluate(Comparison(null)).ShouldBeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    void should_admit_a_valid_ordinal(int ordinal) => SceneGuards.CanEvaluate(Comparison(ordinal)).ShouldBeTrue();

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    [InlineData(0.5)]
    void should_refuse_an_ordinal_outside_the_declared_members(double ordinal)
    {
        SceneGuards.CanEvaluate(Comparison(ordinal)).ShouldBeFalse();
        SceneGuards.EnumerationMembershipDiagnostic(Comparison(ordinal)).ShouldEqual(SceneGuards.UnknownEnumerationLiteralDiagnosticCode);
    }

    [Fact]
    void should_refuse_malformed_enumerations_via_the_runtime_capability_predicate()
    {
        foreach (var enumeration in new object[] { Array.Empty<string>(), new[] { "open", "open" }, new object[] { "open", 1 }, "open" })
        {
            var comparison = Comparison("open");
            comparison["enumeration"] = enumeration;
            GuardConditions.CanEvaluate(JsonSerializer.SerializeToElement(comparison, _options)).ShouldBeFalse();
            SceneGuards.CanEvaluate(comparison).ShouldBeFalse();
        }
    }

    [Fact]
    void should_apply_stage_membership_validation_to_serialized_action_admission()
    {
        var properties = new Dictionary<string, object?>
        {
            ["label"] = "Close",
            ["alternatives"] = new[] { new { command = "CloseWorkItem", condition = Comparison("archived") } }
        };
        SceneGuards.CanRunAction(properties).ShouldBeFalse();
        SceneGuards.HasUnevaluableGuard(JsonSerializer.Serialize(new { componentName = "core:guarded-action", properties }, _options)).ShouldBeTrue();
    }

    [Fact]
    void should_validate_membership_recursively_in_typed_logical_guards()
    {
        var known = new ComparisonGuardCondition(new("item.status"), GuardOperator.Equal, "open", ["open", "closed"]);
        var unknown = known with { Right = "archived" };
        foreach (var operation in new[] { LogicalGuardOperator.And, LogicalGuardOperator.Or })
        {
            foreach (var guard in new[] { new LogicalGuardCondition(known, operation, unknown), new LogicalGuardCondition(unknown, operation, known) })
            {
                SceneGuards.CanEvaluate(guard).ShouldBeFalse();
                SceneGuards.EnumerationMembershipDiagnostic(guard).ShouldEqual(SceneGuards.UnknownEnumerationLiteralDiagnosticCode);
                SceneGuards.CanEvaluate(JsonSerializer.SerializeToElement<GuardCondition>(guard, _options)).ShouldBeFalse();
            }
        }
    }
}
