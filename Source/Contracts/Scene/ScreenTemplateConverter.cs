// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneScreens = Cratis.Scene.Model.Screens;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Converts a compiled Screenplay <see cref="ScreenplaySyntax.ScreenTemplateSyntax"/> into a
/// <see cref="SceneScreens.ScreenTemplate"/> - part of Cratis/Stage#37.
/// </summary>
/// <remarks>
/// A screen template is a reusable shape inside the application's shell, declared at module level.
/// <c language="csharp">fits slot &lt;name&gt;</c> names the slot of its parent - the application <c language="csharp">layout</c> for a module's
/// template, an enclosing template's slot for a deeper one - that it fills, and carries straight through to
/// <see cref="SceneScreens.ScreenTemplate.FitsSlot"/>; it is optional, and a template that declares none is
/// placed by whatever renders it. <see cref="SceneScreens.ScreenTemplate.Content"/> is always left unset:
/// Screenplay templates declare slots and an arrangement only, never chrome of their own - the content comes
/// from the <see cref="SceneScreens.Screen"/> that fills them.
/// </remarks>
public static class ScreenTemplateConverter
{
    /// <summary>
    /// Converts a <see cref="ScreenplaySyntax.ScreenTemplateSyntax"/> into a <see cref="SceneScreens.ScreenTemplate"/>.
    /// </summary>
    /// <param name="template">The <see cref="ScreenplaySyntax.ScreenTemplateSyntax"/> to convert.</param>
    /// <returns>The converted <see cref="SceneScreens.ScreenTemplate"/>.</returns>
    public static SceneScreens.ScreenTemplate Convert(ScreenplaySyntax.ScreenTemplateSyntax template) => Convert(template, null);

    /// <summary>
    /// Converts a <see cref="ScreenplaySyntax.ScreenTemplateSyntax"/>, resolving behaviors used by its own slot content.
    /// </summary>
    /// <param name="template">The <see cref="ScreenplaySyntax.ScreenTemplateSyntax"/> to convert.</param>
    /// <param name="behaviors">What a <c language="csharp">uses</c> clause inside the template's content resolves against.</param>
    /// <returns>The converted <see cref="SceneScreens.ScreenTemplate"/>.</returns>
    public static SceneScreens.ScreenTemplate Convert(ScreenplaySyntax.ScreenTemplateSyntax template, BehaviorScope? behaviors) =>
        new(
            template.Name,
            template.FitsSlot,
            SlotConverter.Convert(template.Slots),
            ArrangementConverter.Convert(template.Arrangement),
            CompositionConverter.Content(template.Name, template.Content, behaviors),
            template.DisplayName,
            template.Description)
        {
            Metadata = CompositionConverter.Metadata(template.TemplateType, template.Category, template.RestrictsScopes, template.Scopes),
            Outlets = [.. template.Outlets.Select(outlet => new SceneScreens.Outlet(outlet.Name))]
        };
}
