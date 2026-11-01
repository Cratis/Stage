// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Separates contract translation from the capabilities needed to produce or serve a runnable Scene.
/// </summary>
public sealed partial class SceneRuntimeAdmission : ScreenplaySyntaxWalker
{
    readonly List<SceneRuntimeIssue> _issues = [];
    readonly Func<string, bool>? _isStringField;
    BehaviorScope? _scope;

    /// <summary>Initializes admission without field type information.</summary>
    public SceneRuntimeAdmission()
    {
    }

    /// <summary>Initializes admission with a known string field resolver.</summary>
    /// <param name="isStringField">Resolves known string fields.</param>
    public SceneRuntimeAdmission(Func<string, bool> isStringField) => _isStringField = isStringField;

    internal SceneRuntimeAdmission(BehaviorScope scope) => _scope = scope;

    /// <summary>
    /// Gets the constructs that the pinned Scene runtime cannot execute faithfully.
    /// </summary>
    public IReadOnlyList<SceneRuntimeIssue> Issues => _issues;

    /// <summary>
    /// Refuses a runnable Scene while leaving contract-only translation and package planning available.
    /// </summary>
    /// <param name="scene">The translated Scene and its source runtime limitations.</param>
    /// <exception cref="UnsupportedGuardedScreenAction">Thrown for a guarded screen action.</exception>
    /// <exception cref="UnsupportedGuardedInteraction">Thrown for guarded interaction alternatives.</exception>
    public static void RequireSupported(SceneApplication scene)
    {
        if (scene.RuntimeIssues.Count == 0) return;
        var issue = scene.RuntimeIssues[0];
        if (issue.Code == UnsupportedGuardedScreenAction.DiagnosticCode)
        {
            throw new UnsupportedGuardedScreenAction(issue.Artifact, issue.Location);
        }

        throw new UnsupportedGuardedInteraction(issue.Artifact, issue.Location);
    }

    /// <inheritdoc/>
    public override void VisitNode(SyntaxNode node)
    {
        switch (node)
        {
            case ScreenGuardedActionSyntax action when !CanRun(action):
                _issues.Add(new(UnsupportedGuardedScreenAction.DiagnosticCode, action.Label, action.Location, new UnsupportedGuardedScreenAction(action.Label, action.Location).Message));
                break;

            // Scene's published engine decides which guard forms it can evaluate. Other forms remain refused.
            case InteractionBindingSyntax binding when (binding.Alternatives.Any() || binding.Otherwise is not null) && !binding.Alternatives.All(alternative => CanEvaluate(alternative.Condition)):
                var interaction = binding.Trigger.ToString();
                _issues.Add(new(UnsupportedGuardedInteraction.DiagnosticCode, interaction, binding.Location, new UnsupportedGuardedInteraction(interaction, binding.Location).Message));
                break;
        }
    }

    bool CanRun(ScreenGuardedActionSyntax action)
    {
        var alternatives = action.Alternatives.ToArray();
        var branches = alternatives.Length > 0 &&
            alternatives.All(alternative => CanEvaluate(alternative.Condition) && !string.IsNullOrEmpty(alternative.Command));
        var otherwise = action.Otherwise?.Outcome switch
        {
            null or ScreenActionOtherwiseOutcome.Hidden => true,
            ScreenActionOtherwiseOutcome.Execute => !string.IsNullOrEmpty(action.Otherwise.Command),
            _ => false,
        };
        return branches && otherwise;
    }

    bool CanEvaluate(ConditionSyntax condition) =>
        SceneGuards.CanEvaluate(SceneGuards.ConvertToCondition(condition, null, _isStringField ?? (_scope is null ? null : _scope.IsStringField)));
}
