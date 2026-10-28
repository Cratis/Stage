// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_SceneRuntimeAdmission;

public class when_typing_contains_for_every_interaction : Specification
{
    static readonly SourceLocation _at = SourceLocation.Start;

    [Theory]
    [InlineData("Click")]
    [InlineData("DoubleClick")]
    [InlineData("Select")]
    [InlineData("Submit")]
    [InlineData("Change")]
    [InlineData("Load")]
    [InlineData("Unload")]
    [InlineData("Enter")]
    [InlineData("Leave")]
    [InlineData("Interval")]
    void should_refuse_non_string_or_unknown_fields_and_admit_known_strings(string trigger)
    {
        foreach (var fieldType in new[] { "String[]", "Int", "Bool", "Object", "Dynamic", "Unknown", "String" })
        {
            var binding = Binding(trigger);
            var admission = new SceneRuntimeAdmission(_ => fieldType == "String");
            admission.VisitNode(binding);
            var converted = BehaviorConverter.Convert(new(null, [], [binding], _at), null, null, "screen", _ => fieldType == "String");
            var condition = converted.Bindings.Single().Alternatives!.Single().Condition;
            if (fieldType == "String")
            {
                admission.Issues.ShouldBeEmpty();
                SceneGuards.CanEvaluate(condition).ShouldBeTrue();
            }
            else
            {
                admission.Issues.Select(issue => issue.Code).ShouldContainOnly(UnsupportedGuardedInteraction.DiagnosticCode);
                SceneGuards.CanEvaluate(condition).ShouldBeFalse();
            }
        }
    }

    static InteractionBindingSyntax Binding(string trigger) => new(
        trigger == "Interval" ? new IntervalInteractionTriggerSyntax(1, IntervalUnit.Seconds, _at) : new BuiltInInteractionTriggerSyntax(Enum.Parse<InteractionTriggerKind>(trigger), _at), null, [], _at)
    {
        Alternatives = [new(new ComparisonConditionSyntax("item.value", ComparisonOperator.Contains, new LiteralExpressionSyntax("open", _at), _at), [new NavigateActionSyntax("Details", _at)], _at)],
        Otherwise = new([], _at),
    };
}
