// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneElements = Cratis.Scene.Model.Elements;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Builds <see cref="SceneElements.ExternalComponent"/> instances with sensible visual defaults - part of
/// Cratis/Stage#37's directive-to-element mapping, where every Screenplay screen directive becomes one
/// component named after its own kind rather than a bespoke <c>SceneElement</c> subtype per widget.
/// </summary>
public static class SceneElementFactory
{
    /// <summary>
    /// Creates an <see cref="SceneElements.ExternalComponent"/>.
    /// </summary>
    /// <param name="id">The element's id, unique within its screen.</param>
    /// <param name="componentName">The component's name, resolved against the active ui profile's package list.</param>
    /// <param name="properties">The directive's own fields, folded into the open properties bag.</param>
    /// <param name="slots">Nested content, keyed by slot name.</param>
    /// <returns>The constructed <see cref="SceneElements.ExternalComponent"/>.</returns>
    public static SceneElements.ExternalComponent Component(
        string id,
        string componentName,
        IReadOnlyDictionary<string, object?>? properties = null,
        IReadOnlyDictionary<string, IReadOnlyList<SceneElements.SceneElement>>? slots = null) =>
        new()
        {
            Id = id,
            Name = id,
            ComponentName = componentName,
            Properties = properties ?? new Dictionary<string, object?>(),
            Slots = slots ?? new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>>(),
        };
}
