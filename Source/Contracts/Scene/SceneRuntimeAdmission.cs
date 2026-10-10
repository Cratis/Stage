// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Separates contract translation from the capabilities needed to produce or serve a runnable Scene.
/// </summary>
public sealed class SceneRuntimeAdmission : ScreenplaySyntaxWalker
{
    readonly List<SceneRuntimeIssue> _issues = [];

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

            // The Stage runtime evaluates guards that compare a field of the subject with a literal (SceneGuards).
            // Anything else stays refused here, with the diagnostic, rather than reaching a runtime that would have
            // to guess.
            case InteractionBindingSyntax binding when (binding.Alternatives.Any() || binding.Otherwise is not null) && !binding.Alternatives.All(alternative => SceneGuards.CanEvaluate(alternative.Condition)):
                var interaction = binding.Trigger.ToString();
                _issues.Add(new(UnsupportedGuardedInteraction.DiagnosticCode, interaction, binding.Location, new UnsupportedGuardedInteraction(interaction, binding.Location).Message));
                break;
        }
    }

    static bool CanRun(ScreenGuardedActionSyntax action)
    {
        var alternatives = action.Alternatives.ToArray();
        var branches = alternatives.Length > 0 &&
            alternatives.All(alternative => SceneGuards.CanEvaluate(alternative.Condition) && !string.IsNullOrEmpty(alternative.Command));
        var otherwise = action.Otherwise?.Outcome switch
        {
            null or ScreenActionOtherwiseOutcome.Hidden => true,
            ScreenActionOtherwiseOutcome.Execute => !string.IsNullOrEmpty(action.Otherwise.Command),
            _ => false,
        };
        return branches && otherwise;
    }
}
