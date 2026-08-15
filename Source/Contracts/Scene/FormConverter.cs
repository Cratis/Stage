// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneCommon = Cratis.Scene.Model.Common;
using SceneModel = Cratis.Scene.Model.Forms;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Converts a compiled Screenplay <see cref="ScreenplaySyntax.FormSyntax"/> into a <see cref="SceneModel.Form"/> -
/// part of Cratis/Stage#37.
/// </summary>
/// <remarks>
/// <see cref="ScreenplaySyntax.FormSyntax.OnSubmit"/> has no home on <see cref="SceneModel.Form"/> and is a
/// known, deliberate gap - not carried through. For <c>populate via query ... by &lt;param&gt;</c>, Screenplay
/// carries only the bare parameter name, not a resolved binding path - this converter treats the parameter
/// name as its own binding path (a value already in scope under that exact name), a documented assumption
/// pending a real multi-screen translation to validate against.
/// </remarks>
public static class FormConverter
{
    /// <summary>
    /// The synthesized binding path used for a <c>populate from item</c> declaration, which carries no
    /// path of its own in Screenplay's grammar.
    /// </summary>
    public const string FromItemPath = "item";

    /// <summary>
    /// Converts a <see cref="ScreenplaySyntax.FormSyntax"/> into a <see cref="SceneModel.Form"/>.
    /// </summary>
    /// <param name="form">The <see cref="ScreenplaySyntax.FormSyntax"/> to convert.</param>
    /// <returns>The converted <see cref="SceneModel.Form"/>.</returns>
    public static SceneModel.Form Convert(ScreenplaySyntax.FormSyntax form) =>
        new(form.Name, form.For, ConvertPopulateSource(form.Populate), [.. form.Fields.Select(ConvertField)]);

    static SceneModel.PopulateSource? ConvertPopulateSource(ScreenplaySyntax.FormPopulateSource? populate) =>
        populate switch
        {
            null => null,
            ScreenplaySyntax.FormPopulateViaQuerySyntax viaQuery => new SceneModel.PopulateViaQuery(
                viaQuery.Query,
                viaQuery.By is null
                    ? []
                    : new Dictionary<string, SceneCommon.BindingExpression> { [viaQuery.By] = new(viaQuery.By) }),
            ScreenplaySyntax.FormPopulateFromItemSyntax => new SceneModel.PopulateFromItem(new SceneCommon.BindingExpression(FromItemPath)),
            _ => throw new UnknownFormPopulateSource(populate.GetType().Name),
        };

    static SceneModel.FormField ConvertField(ScreenplaySyntax.FormFieldSyntax field) =>
        new(field.Property, field.From, field.ComposeUsing, field.Label);
}
