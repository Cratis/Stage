// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using SceneCommon = Cratis.Scene.Model.Common;
using SceneModel = Cratis.Scene.Model.Forms;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Converts a compiled Screenplay <see cref="ScreenplaySyntax.FormSyntax"/> into a <see cref="SceneModel.Form"/> -
/// part of Cratis/Stage#37.
/// </summary>
/// <remarks>
/// For <c language="csharp">populate via query ... by &lt;param&gt;</c>, Screenplay
/// carries only the bare parameter name, not a resolved binding path - this converter treats the parameter
/// name as its own binding path (a value already in scope under that exact name), a documented assumption
/// pending a real multi-screen translation to validate against.
/// </remarks>
public static class FormConverter
{
    /// <summary>
    /// The synthesized binding path used for a <c language="csharp">populate from item</c> declaration, which carries no
    /// path of its own in Screenplay's grammar.
    /// </summary>
    public const string FromItemPath = "item";

    /// <summary>
    /// Converts a <see cref="ScreenplaySyntax.FormSyntax"/> into a <see cref="SceneModel.Form"/>.
    /// </summary>
    /// <param name="form">The <see cref="ScreenplaySyntax.FormSyntax"/> to convert.</param>
    /// <returns>The converted <see cref="SceneModel.Form"/>.</returns>
    public static SceneModel.Form Convert(ScreenplaySyntax.FormSyntax form) =>
        new(
            form.Name,
            form.For,
            ConvertPopulateSource(form.Populate),
            [.. form.Fields.Select(ConvertField)],
            ScreenplayRichSyntax.GenerationMode(form),
            ScreenplayRichSyntax.Layout(form))
        {
            OnSubmit = form.OnSubmit is { } navigate
                ? [Submit(navigate)]
                : []
        };

    /// <summary>
    /// Converts a form while omitting an unsupported population source.
    /// </summary>
    /// <param name="form">The form to convert.</param>
    /// <param name="diagnostics">The collector, or null for legacy throwing behavior.</param>
    /// <param name="path">The owning form path.</param>
    /// <returns>The form with its supported fields and population source.</returns>
    public static SceneModel.Form Convert(ScreenplaySyntax.FormSyntax form, ConversionDiagnostics? diagnostics, string path)
    {
        if (diagnostics is null || form.Populate is null or ScreenplaySyntax.FormPopulateViaQuerySyntax or ScreenplaySyntax.FormPopulateFromItemSyntax) return Convert(form);
        var construct = $"form populate source {form.Populate.GetType().Name}";
        diagnostics.Report(new("STAGE-SCENE-FORM-001", DiagnosticSeverity.Error, construct, path, form.Populate.Location, $"Unsupported {construct} was omitted from '{path}'."));
        return Convert(form with { Populate = null });
    }

    static Cratis.Scene.Model.Interactions.NavigateAction Submit(ScreenplaySyntax.ScreenNavigateSyntax navigate) => new(navigate.Screen)
    {
        Arguments = navigate.By is { } by ? [new(by, new SceneCommon.BindingExpression(by))] : []
    };

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
        new(field.Property, field.From, field.ComposeUsing, field.Label, ScreenplayRichSyntax.FieldPlacement(field));
}
