// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Elements;
using Cratis.Scene.Model.Screens;
using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Adds an advisory <c language="json">rowAction</c> selection hint to raw Scene actions.
/// </summary>
/// <remarks>
/// This hint is not semantic intent. At rendering and runtime boundaries, proven creation
/// (<c language="json">x-creates</c> / <c language="json">createsEntity</c>) takes precedence: a creation never acts on a selected row.
/// </remarks>
/// <param name="syntax">The authored application syntax.</param>
internal sealed class CollectionRowActions(ApplicationSyntax syntax)
{
    readonly SliceSyntax[] _slices = [.. Slices(syntax.Modules.SelectMany(module => module.Features))];

    public Screen Bind(Screen screen)
    {
        var elements = screen.SlotContent.Values.SelectMany(content => content).Concat(screen.Contributions.Select(contribution => contribution.Content)).SelectMany(Descend).ToArray();
        var properties = elements.Where(element => element.ComponentName == "core:table")
            .SelectMany(table => RowProperties(Text(table, SceneElementProperties.TypeName))).ToHashSet(StringComparer.Ordinal);
        return screen with
        {
            SlotContent = screen.SlotContent.ToDictionary(slot => slot.Key, slot => (IReadOnlyList<SceneElement>)[.. slot.Value.Select(element => Bind(element, properties))], StringComparer.Ordinal),
            Contributions = [.. screen.Contributions.Select(contribution => contribution with { Content = Bind(contribution.Content, properties) })]
        };
    }

    static IEnumerable<ExternalComponent> Descend(SceneElement element) => element is ExternalComponent component
        ? [component, .. component.Slots.Values.SelectMany(content => content).SelectMany(Descend)]
        : [];

    static string? Text(ExternalComponent element, string name) => element.Properties.GetValueOrDefault(name) as string;

    static IEnumerable<SliceSyntax> Slices(IEnumerable<FeatureSyntax> features) => features.SelectMany(feature => feature.Slices.Concat(Slices(feature.Features)));

    SceneElement Bind(SceneElement element, HashSet<string> rowProperties)
    {
        if (element is not ExternalComponent component) return element;
        component = component with
        {
            Slots = component.Slots.ToDictionary(slot => slot.Key, slot => (IReadOnlyList<SceneElement>)[.. slot.Value.Select(child => Bind(child, rowProperties))], StringComparer.Ordinal)
        };
        if (component.ComponentName != "core:action" || Text(component, "command") is not { } command) return component;
        var identifiers = _slices.SelectMany(slice => slice.Commands).Where(candidate => candidate.Name == command)
            .SelectMany(candidate => candidate.Properties).Where(property => !property.IsGenerated && !property.Type.IsOptional && property.IsIdentifier);
        if (!identifiers.Any(property => rowProperties.Contains(property.Name))) return component;
        return component with { Properties = new Dictionary<string, object?>(component.Properties, StringComparer.Ordinal) { ["rowAction"] = true } };
    }

    IEnumerable<string> RowProperties(string? target) => _slices.SelectMany(slice => slice.ReadModels ?? [])
        .Where(model => model.Name == target || target?.EndsWith($".{model.Name}", StringComparison.Ordinal) == true)
        .SelectMany(model => model.Properties.Select(property => property.Name));
}
