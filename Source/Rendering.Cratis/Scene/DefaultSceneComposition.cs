// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.Semantics;
using SceneElements = Cratis.Scene.Model.Elements;
using SceneScreens = Cratis.Scene.Model.Screens;

namespace Cratis.Stage.Rendering.Cratis.Scene;

/// <summary>
/// Composes a default screen for an application that declares none.
/// </summary>
/// <remarks>
/// <para>
/// An application whose Screenplay declares screens carries its own composition, and that is always
/// preferred. This is the fallback for everything else: without it a generated application ships a frontend
/// shell with nothing in it, and the only way to exercise the backend that was just generated is to write
/// the screen by hand.
/// </para>
/// <para>
/// It composes one command form per admitted command, bound by the command's semantic name. That is the
/// component's whole purpose: it reads the command's own property descriptors and picks a field per
/// property, so the form follows the command rather than going stale when a property is added.
/// </para>
/// <para>
/// Keyed queries are deliberately <b>not</b> composed here. The released single-result component takes its
/// argument from the host, which has committed it - there is no editable input binding yet (Cratis/Scene#39),
/// so a composed keyed lookup could only ever render its idle state. Emitting one would put a permanently
/// inert element on every generated screen and suggest a capability that does not exist.
/// </para>
/// </remarks>
internal static class DefaultSceneComposition
{
    /// <summary>
    /// The component that renders a command.
    /// </summary>
    const string CommandFormComponent = "Cratis.Components:commandForm";

    /// <summary>
    /// Composes the default screen for an application.
    /// </summary>
    /// <param name="context">The indexed semantic application.</param>
    /// <returns>The composed Scene application, or <c language="csharp">null</c> when there is nothing to compose.</returns>
    public static SceneApplication? Create(SemanticApplicationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var commands = context.Commands.Values
            .Select(_ => _.Name)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (commands.Length == 0)
        {
            return null;
        }

        var layout = DefaultLayout.Create();
        var elements = commands.Select(CommandForm).ToArray();
        var screen = new SceneScreens.Screen(
            context.Application.Name,
            layout.Name,
            new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>>(StringComparer.Ordinal)
            {
                [DefaultLayout.ContentSlotName] = elements
            },
            [],
            []);

        return new SceneApplication([], [], [layout], [], [], [screen]);
    }

    static SceneElements.SceneElement CommandForm(string command) =>
        new SceneElements.ExternalComponent
        {
            Id = command,
            Name = command,
            ComponentName = CommandFormComponent,
            Properties = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["command"] = command
            }
        };
}
