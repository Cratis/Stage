// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneScreens = Cratis.Scene.Model.Screens;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Converts a compiled Screenplay <see cref="ScreenplaySyntax.DialogTemplateSyntax"/> into a
/// <see cref="SceneScreens.DialogTemplate"/> - part of Cratis/Stage#37.
/// </summary>
/// <remarks>
/// A dialog template is a screen template in everything but one respect: it fills no slot, because it opens
/// <em>over</em> the application rather than sitting inside it. Screenplay makes that structural - there is no
/// <c language="csharp">fits slot</c> on <see cref="ScreenplaySyntax.DialogTemplateSyntax"/> to carry - and so does Scene, which
/// is why this converter is a near-duplicate of <see cref="ScreenTemplateConverter"/> rather than a shared one
/// with a flag. <see cref="SceneScreens.DialogTemplate.Content"/> is always left unset for the same reason it
/// is on a screen template: Screenplay declares slots and an arrangement, and the filling
/// <see cref="SceneScreens.Screen"/> brings the content.
/// </remarks>
public static class DialogTemplateConverter
{
    /// <summary>
    /// Converts a <see cref="ScreenplaySyntax.DialogTemplateSyntax"/> into a <see cref="SceneScreens.DialogTemplate"/>.
    /// </summary>
    /// <param name="template">The <see cref="ScreenplaySyntax.DialogTemplateSyntax"/> to convert.</param>
    /// <returns>The converted <see cref="SceneScreens.DialogTemplate"/>.</returns>
    public static SceneScreens.DialogTemplate Convert(ScreenplaySyntax.DialogTemplateSyntax template) => Convert(template, null);

    /// <summary>
    /// Converts a <see cref="ScreenplaySyntax.DialogTemplateSyntax"/>, resolving behaviors used by its own slot content.
    /// </summary>
    /// <param name="template">The <see cref="ScreenplaySyntax.DialogTemplateSyntax"/> to convert.</param>
    /// <param name="behaviors">What a <c language="csharp">uses</c> clause inside the template's content resolves against.</param>
    /// <returns>The converted <see cref="SceneScreens.DialogTemplate"/>.</returns>
    public static SceneScreens.DialogTemplate Convert(ScreenplaySyntax.DialogTemplateSyntax template, BehaviorScope? behaviors) =>
        new(
            template.Name,
            SlotConverter.Convert(template.Slots),
            ArrangementConverter.Convert(template.Arrangement),
            CompositionConverter.Content(template.Name, template.Content, behaviors),
            template.DisplayName,
            template.Description)
        {
            Metadata = CompositionConverter.Metadata(template.TemplateType, template.Category, template.RestrictsScopes, template.Scopes)
        };
}
