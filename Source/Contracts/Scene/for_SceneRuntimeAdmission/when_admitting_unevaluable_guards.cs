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

    static ScreenGuardedActionSyntax Action(ConditionSyntax condition) =>
        new("Close", [new ScreenActionAlternativeSyntax(condition, "CloseWorkItem", _at)], _at) { Otherwise = new ScreenActionOtherwiseSyntax(ScreenActionOtherwiseOutcome.Hidden, null, _at) };

    static InteractionBindingSyntax Interaction(ConditionSyntax condition) =>
        new(new BuiltInInteractionTriggerSyntax(InteractionTriggerKind.DoubleClick, _at), null, [], _at)
        {
            Alternatives = [new InteractionAlternativeSyntax(condition, [new NavigateActionSyntax("WorkItemDetails", _at)], _at)],
            Otherwise = new InteractionOtherwiseSyntax([], _at),
        };

    static string[] Issues(SyntaxNode node)
    {
        var admission = new SceneRuntimeAdmission();
        admission.VisitNode(node);
        return [.. admission.Issues.Select(_ => _.Code)];
    }
}
