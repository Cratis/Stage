// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneElements = Cratis.Scene.Model.Elements;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Collects every component name a translated <see cref="SceneApplication"/> references - part of
/// Cratis/Stage#39, the input a target's package resolution runs over.
/// </summary>
/// <remarks>
/// A component name reaches the model through <see cref="SceneElements.ExternalComponent.ComponentName"/> and
/// nowhere else - the typed element hierarchy carries no other name to resolve - so this walks every element
/// tree the application holds and reads that one member. Content lives in four places: a screen's slot
/// content, a screen's contributions, and the chrome a screen or dialog template brings with it. Templates
/// translated from Screenplay carry no chrome, but a template supplied by a blueprint package does, and
/// missing its components would make the resolution report look clean while the shell rendered empty.
/// </remarks>
public static class ComponentReferences
{
    /// <summary>
    /// Collects the distinct component names an application references.
    /// </summary>
    /// <param name="application">The <see cref="SceneApplication"/> to walk.</param>
    /// <returns>The component names, sorted, so a plan built from the same application is always identical.</returns>
    public static IReadOnlyList<string> Collect(SceneApplication application) =>
        [.. Elements(application).OfType<SceneElements.ExternalComponent>()
            .Select(component => component.ComponentName)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)];

    static IEnumerable<SceneElements.SceneElement> Elements(SceneApplication application)
    {
        var roots = application.Screens.SelectMany(screen => screen.SlotContent.Values.SelectMany(content => content))
            .Concat(application.Screens.SelectMany(screen => screen.Contributions.Select(contribution => contribution.Content)))
            .Concat(application.ScreenTemplates.SelectMany(template => Chrome(template.Content)))
            .Concat(application.DialogTemplates.SelectMany(template => Chrome(template.Content)));

        return roots.SelectMany(Descend);
    }

    static IEnumerable<SceneElements.SceneElement> Chrome(IReadOnlyDictionary<string, IReadOnlyList<SceneElements.SceneElement>>? content) =>
        (content ?? new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>>()).Values.SelectMany(elements => elements);

    static IEnumerable<SceneElements.SceneElement> Descend(SceneElements.SceneElement element) =>
        [element, .. Children(element).SelectMany(Descend)];

    static IEnumerable<SceneElements.SceneElement> Children(SceneElements.SceneElement element) =>
        element switch
        {
            SceneElements.ExternalComponent component => component.Slots.Values.SelectMany(elements => elements),
            SceneElements.Panel panel => panel.Children,
            SceneElements.ItemsControl items => [items.ItemTemplate],
            SceneElements.ContentControl content => [content.Content],
            _ => [],
        };
}
