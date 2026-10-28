// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Scene;
using SceneContribution = Cratis.Scene.Model.ContributionPoints.Contribution;
using SceneElements = Cratis.Scene.Model.Elements;
using SceneScreens = Cratis.Scene.Model.Screens;

namespace Cratis.Stage.Host;

/// <summary>
/// Produces the runnable subset of an authored Scene while preserving unsupported constructs as reported issues.
/// </summary>
internal static class SafeSceneApplication
{
    /// <summary>
    /// Determines whether a runtime issue is fail-closed by the translated/sanitized Scene payload.
    /// </summary>
    /// <param name="issue">The runtime issue.</param>
    /// <returns><c language="csharp">true</c> when Stage may serve the remaining Scene safely.</returns>
    internal static bool CanServeWithoutUnsafeBehavior(SceneRuntimeIssue issue) =>
        string.Equals(issue.Code, UnsupportedGuardedScreenAction.DiagnosticCode, StringComparison.Ordinal) ||
        string.Equals(issue.Code, UnsupportedGuardedInteraction.DiagnosticCode, StringComparison.Ordinal);

    /// <summary>
    /// Removes Scene elements the current browser runtime cannot execute faithfully.
    /// </summary>
    /// <param name="scene">The translated authored Scene.</param>
    /// <returns>The runnable Scene subset.</returns>
    internal static SceneApplication From(SceneApplication scene) => scene with
    {
        ScreenTemplates = [.. scene.ScreenTemplates.Select(Safe)],
        DialogTemplates = [.. scene.DialogTemplates.Select(Safe)],
        Screens = [.. scene.Screens.Select(Safe)]
    };

    static SceneScreens.ScreenTemplate Safe(SceneScreens.ScreenTemplate template) => template with
    {
        Content = Safe(template.Content)
    };

    static SceneScreens.DialogTemplate Safe(SceneScreens.DialogTemplate template) => template with
    {
        Content = Safe(template.Content)
    };

    static SceneScreens.Screen Safe(SceneScreens.Screen screen) => screen with
    {
        SlotContent = Safe(screen.SlotContent),
        Contributions = [.. screen.Contributions.Select(Safe)]
    };

    static SceneContribution Safe(SceneContribution contribution) => contribution with
    {
        Content = Safe(contribution.Content) ?? contribution.Content
    };

    static Dictionary<string, IReadOnlyList<SceneElements.SceneElement>> Safe(IReadOnlyDictionary<string, IReadOnlyList<SceneElements.SceneElement>>? slots) =>
        (slots ?? new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>>()).ToDictionary(slot => slot.Key, slot => Safe(slot.Value), StringComparer.Ordinal);

    static IReadOnlyList<SceneElements.SceneElement> Safe(IReadOnlyList<SceneElements.SceneElement> elements) =>
        [.. elements.Select(Safe).OfType<SceneElements.SceneElement>()];

    static SceneElements.SceneElement? Safe(SceneElements.SceneElement element)
    {
        if (IsUnsupportedGuardedAction(element))
        {
            return null;
        }

        return element is SceneElements.ExternalComponent component
            ? component with { Slots = Safe(component.Slots) }
            : element;
    }

    // A guarded action the Stage runtime can evaluate is served as authored; only one it cannot is left out.
    static bool IsUnsupportedGuardedAction(SceneElements.SceneElement element) =>
        element is SceneElements.ExternalComponent component && string.Equals(component.ComponentName, "core:action", StringComparison.Ordinal) &&
        (element.Properties.ContainsKey("alternatives") || element.Properties.ContainsKey("otherwise")) &&
        !SceneGuards.CanRunAction(element.Properties);
}
