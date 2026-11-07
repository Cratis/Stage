// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_SceneRuntimeAdmission;

/// <summary>
/// Guards the Stage runtime cannot evaluate. Screenplay refuses most of them while compiling, so these are built as
/// syntax directly: whatever reaches admission, an unevaluable guard is refused with its diagnostic, not run.
/// </summary>
public class when_admitting_unevaluable_guards : Specification
{
    static readonly SourceLocation _at = SourceLocation.Start;
    static readonly ConditionSyntax _evaluable = new ComparisonConditionSyntax("item.status", ComparisonOperator.Equal, new LiteralExpressionSyntax("open", _at), _at);
    static readonly ConditionSyntax _againstAPath = new ComparisonConditionSyntax("item.status", ComparisonOperator.Equal, new PathExpressionSyntax("item.other", _at), _at);
    static readonly ConditionSyntax _notAboutTheItem = new ComparisonConditionSyntax("state.status", ComparisonOperator.Equal, new LiteralExpressionSyntax("open", _at), _at);

    [Fact] void should_admit_an_evaluable_guarded_action() => Issues(Action(_evaluable)).ShouldBeEmpty();
    [Fact] void should_refuse_a_guard_compared_with_a_path() => Issues(Action(_againstAPath)).ShouldContainOnly(UnsupportedGuardedScreenAction.DiagnosticCode);
    [Fact] void should_refuse_a_guard_not_about_the_item() => Issues(Action(_notAboutTheItem)).ShouldContainOnly(UnsupportedGuardedScreenAction.DiagnosticCode);
    [Fact] void should_refuse_a_logical_guard_with_an_unevaluable_side() => Issues(Action(new LogicalConditionSyntax(_evaluable, LogicalOperator.And, _againstAPath, _at))).ShouldContainOnly(UnsupportedGuardedScreenAction.DiagnosticCode);
    [Fact] void should_refuse_an_unknown_fallback() => Issues(Action(_evaluable) with { Otherwise = new ScreenActionOtherwiseSyntax(ScreenActionOtherwiseOutcome.Unknown, null, _at) }).ShouldContainOnly(UnsupportedGuardedScreenAction.DiagnosticCode);
    [Fact] void should_refuse_an_executing_fallback_without_a_command() => Issues(Action(_evaluable) with { Otherwise = new ScreenActionOtherwiseSyntax(ScreenActionOtherwiseOutcome.Execute, null, _at) }).ShouldContainOnly(UnsupportedGuardedScreenAction.DiagnosticCode);
    [Fact] void should_admit_an_evaluable_guarded_interaction() => Issues(Interaction(_evaluable)).ShouldBeEmpty();
    [Fact] void should_refuse_an_unevaluable_guarded_interaction() => Issues(Interaction(_againstAPath)).ShouldContainOnly(UnsupportedGuardedInteraction.DiagnosticCode);

    [Theory]
    [InlineData(1)]
    [InlineData(true)]
    [InlineData(null)]
    void should_refuse_contains_with_a_non_string_literal_on_both_surfaces(object? literal)
    {
        var condition = new ComparisonConditionSyntax("item.tags", ComparisonOperator.Contains, new LiteralExpressionSyntax(literal, _at), _at);
        Issues(Action(condition)).ShouldContainOnly(UnsupportedGuardedScreenAction.DiagnosticCode);
        Issues(Interaction(condition)).ShouldContainOnly(UnsupportedGuardedInteraction.DiagnosticCode);
    }

    [Fact]
    void should_admit_contains_with_a_known_string_field_on_both_surfaces()
    {
        var condition = new ComparisonConditionSyntax("item.title", ComparisonOperator.Contains, new LiteralExpressionSyntax("open", _at), _at);
        Issues(Action(condition), path => path == "item.title").ShouldBeEmpty();
        Issues(Interaction(condition), path => path == "item.title").ShouldBeEmpty();
    }

    [Fact]
    void should_refuse_contains_with_an_unknown_field_type_on_both_surfaces()
    {
        var condition = new ComparisonConditionSyntax("item.title", ComparisonOperator.Contains, new LiteralExpressionSyntax("open", _at), _at);
        Issues(Action(condition)).ShouldContainOnly(UnsupportedGuardedScreenAction.DiagnosticCode);
        Issues(Interaction(condition)).ShouldContainOnly(UnsupportedGuardedInteraction.DiagnosticCode);
    }

    [Fact]
    void should_admit_null_equality_on_both_surfaces()
    {
        var condition = new ComparisonConditionSyntax("item.status", ComparisonOperator.Equal, new LiteralExpressionSyntax(null, _at), _at);
        Issues(Action(condition)).ShouldBeEmpty();
        Issues(Interaction(condition)).ShouldBeEmpty();
    }

    static ScreenGuardedActionSyntax Action(ConditionSyntax condition) =>
        new("Close", [new ScreenActionAlternativeSyntax(condition, "CloseWorkItem", _at)], _at) { Otherwise = new ScreenActionOtherwiseSyntax(ScreenActionOtherwiseOutcome.Hidden, null, _at) };

    static InteractionBindingSyntax Interaction(ConditionSyntax condition) =>
        new(new BuiltInInteractionTriggerSyntax(InteractionTriggerKind.DoubleClick, _at), null, [], _at)
        {
            Alternatives = [new InteractionAlternativeSyntax(condition, [new NavigateActionSyntax("WorkItemDetails", _at)], _at)],
            Otherwise = new InteractionOtherwiseSyntax([], _at),
        };

    static string[] Issues(SyntaxNode node, Func<string, bool>? isStringField = null)
    {
        var admission = isStringField is null ? new SceneRuntimeAdmission() : new SceneRuntimeAdmission(isStringField);
        admission.VisitNode(node);
        return [.. admission.Issues.Select(_ => _.Code)];
    }
}
