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
/// <c>fits slot</c> on <see cref="ScreenplaySyntax.DialogTemplateSyntax"/> to carry - and so does Scene, which
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
    public static SceneScreens.DialogTemplate Convert(ScreenplaySyntax.DialogTemplateSyntax template) =>
        new(
            template.Name,
            SlotConverter.Convert(template.Slots),
            ArrangementConverter.Convert(template.Arrangement));
}
