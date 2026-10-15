// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using Cratis.Scene.Engine.SampleData;
using SceneSampleData = Cratis.Scene.Model.SampleData;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Converts Screenplay's <c language="csharp">sample data</c> packs and a <c language="csharp">ui profile</c>'s sample data selection into Scene's
/// <see cref="SceneSampleData.SampleDataPack"/> and <see cref="SceneSampleData.SampleDataSelection"/>.
/// </summary>
/// <remarks>
/// <para>
/// Sample data is design and preview time only. It reaches the Scene model so the designer and the Stage preview resolve rows the same
/// way - bound pack, active pack, blueprint default, generated - but a production build never includes it, and it never reaches the
/// backend or the executable semantic model.
/// </para>
/// <para>
/// A Screenplay pack is named like any declaration (<c language="csharp">NordicDemo</c>); Scene identifies a pack by a lower-case id
/// (<c language="csharp">nordic-demo</c>), so <see cref="IdOf"/> derives one deterministically and every reference to the pack - the active
/// pack, a binding - uses the same id. A pack the author declares is owned by <c language="csharp">user</c>. <c language="csharp">active blueprint &lt;id&gt;</c>
/// names a pack a blueprint ships: its id is kept as written and the pack is resolved by the build, not here. A pack without a title is
/// shown by its name, and one without a locale follows Scene's default locale. A row field keeps its authored literal: a string stays
/// text, a number stays a whole number or a decimal, <c language="csharp">true</c>/<c language="csharp">false</c> stay Booleans and <c language="csharp">null</c> stays null.
/// </para>
/// <para>
/// Whether a pack's rows fit the read model is the Screenplay compiler's check (<c language="csharp">PLAY0710</c>, <c language="csharp">PLAY0711</c>); whether the pack and
/// the selection are well formed as Scene models them is Scene's, and <see cref="Validate"/> runs it unmodified.
/// </para>
/// </remarks>
public static class SampleDataConverter
{
    const string UserOwner = SampleDataRules.UserOwner;

    /// <summary>
    /// Converts every pack the application declares.
    /// </summary>
    /// <param name="application">The compiled <see cref="ScreenplaySyntax.ApplicationSyntax"/>.</param>
    /// <returns>The <see cref="SceneSampleData.SampleDataPack"/>s, in declaration order.</returns>
    public static IReadOnlyList<SceneSampleData.SampleDataPack> Packs(ScreenplaySyntax.ApplicationSyntax application) =>
        [.. application.SampleDataPacks.Select(Convert)];

    /// <summary>
    /// Converts the sample data selection of a <c language="csharp">ui profile</c>.
    /// </summary>
    /// <param name="selection">The <see cref="ScreenplaySyntax.SampleDataSelectionSyntax"/>, or <c language="csharp">null</c> when the profile has none.</param>
    /// <returns>The <see cref="SceneSampleData.SampleDataSelection"/>, or <c language="csharp">null</c> when the profile has none.</returns>
    public static SceneSampleData.SampleDataSelection? Selection(ScreenplaySyntax.SampleDataSelectionSyntax? selection) =>
        selection is null
            ? null
            : new SceneSampleData.SampleDataSelection(
                [.. selection.Mappings.Select(mapping => new SceneSampleData.SampleDataMapping(mapping.Shape, mapping.ReadModel))],
                [.. selection.Bindings.Select(binding => new SceneSampleData.SampleDataBinding(binding.ReadModel, IdOf(binding.Pack)))],
                selection.Active is null ? null : IdOf(selection.Active));

    /// <summary>
    /// Derives the Scene pack id of a reference: a blueprint pack keeps the id the blueprint gave it, a document's own pack gets the derived id.
    /// </summary>
    /// <param name="reference">The reference as authored.</param>
    /// <returns>The Scene pack id.</returns>
    public static string IdOf(ScreenplaySyntax.SampleDataPackReferenceSyntax reference) => reference.IsBlueprint ? reference.Name : IdOf(reference.Name);

    /// <summary>
    /// Collects the ids of the blueprint packs a document's profiles name.
    /// </summary>
    /// <param name="application">The compiled <see cref="ScreenplaySyntax.ApplicationSyntax"/>.</param>
    /// <returns>The distinct blueprint pack ids, in declaration order.</returns>
    public static IReadOnlyList<string> BlueprintPacks(ScreenplaySyntax.ApplicationSyntax application) =>
        [.. (application.UiProfiles ?? [])
            .Where(profile => profile.SampleData is not null)
            .SelectMany(profile => (profile.SampleData!.Active is { } active ? [active] : Enumerable.Empty<ScreenplaySyntax.SampleDataPackReferenceSyntax>()).Concat(profile.SampleData.Bindings.Select(binding => binding.Pack)))
            .Where(reference => reference.IsBlueprint)
            .Select(reference => reference.Name)
            .Distinct(StringComparer.Ordinal)];

    /// <summary>
    /// Derives the Scene pack id from a Screenplay pack name: lower-case words separated by hyphens.
    /// </summary>
    /// <param name="name">The pack name as authored, such as <c language="csharp">NordicDemo</c>.</param>
    /// <returns>The id, such as <c language="csharp">nordic-demo</c>.</returns>
    public static string IdOf(string name)
    {
        var builder = new StringBuilder(name.Length + 4);
        for (var index = 0; index < name.Length; index++)
        {
            var current = name[index];
            if (char.IsUpper(current) && index > 0 && (char.IsLower(name[index - 1]) || char.IsDigit(name[index - 1]) || (index + 1 < name.Length && char.IsLower(name[index + 1]))))
            {
                builder.Append('-');
            }

            builder.Append(char.IsLetterOrDigit(current) ? char.ToLowerInvariant(current) : '-');
        }

        return builder.ToString();
    }

    /// <summary>
    /// Runs Scene's validation of packs and selections over what the application declares.
    /// </summary>
    /// <param name="application">The translated <see cref="SceneApplication"/>.</param>
    /// <returns>The findings, if any.</returns>
    public static IEnumerable<RenderFinding> Validate(SceneApplication application)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pack in application.SampleDataPacks)
        {
            if (!ids.Add(pack.Id))
            {
                yield return new RenderFinding(RenderFindingKind.SampleDataInvalid, pack.Id, $"Sample data pack '{pack.Id}' is declared more than once (two pack names derive the same id)");
            }

            foreach (var problem in SampleDataValidation.ValidatePack(pack))
            {
                yield return new RenderFinding(RenderFindingKind.SampleDataInvalid, pack.Id, problem);
            }
        }

        foreach (var profile in application.UiProfiles.Where(profile => profile.SampleData is not null))
        {
            // A blueprint's pack is resolved by the build against the packages the profile activates, so it is available here by definition.
            var blueprint = application.BlueprintSampleDataPacks.Where(id => !ids.Contains(id)).Select(id => new SceneSampleData.SampleDataPack(id, id, SampleLocales.DefaultLocale, $"blueprint:{id}", []));
            foreach (var problem in SampleDataValidation.ValidateSelection(profile.SampleData!, [.. application.SampleDataPacks, .. blueprint]))
            {
                yield return new RenderFinding(RenderFindingKind.SampleDataInvalid, profile.Name, problem);
            }
        }
    }

    static SceneSampleData.SampleDataPack Convert(ScreenplaySyntax.SampleDataPackSyntax pack) =>
        new(
            IdOf(pack.Name),
            pack.Title ?? pack.Name,
            pack.Locale ?? SampleLocales.DefaultLocale,
            UserOwner,
            [.. pack.Sets.Select(set => new SceneSampleData.SampleDataSet(set.Shape, [.. set.Rows.Select(Convert)]))]);

    static JsonObject Convert(ScreenplaySyntax.SampleDataRowSyntax row)
    {
        var result = new JsonObject();
        foreach (var field in row.Fields)
        {
            result[field.Name] = field.Kind == ScreenplaySyntax.SampleDataValueKind.Null ? null : Value(field);
        }

        return result;
    }

    static JsonValue Value(ScreenplaySyntax.SampleDataFieldSyntax field) => field.Kind switch
    {
        ScreenplaySyntax.SampleDataValueKind.Flag => JsonValue.Create(string.Equals(field.Value, "true", StringComparison.Ordinal)),
        ScreenplaySyntax.SampleDataValueKind.Number => Number(field.Value ?? "0"),
        _ => JsonValue.Create(field.Value ?? string.Empty)
    };

    static JsonValue Number(string token) =>
        long.TryParse(token, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var whole)
            ? JsonValue.Create(whole)
            : JsonValue.Create(decimal.Parse(token, NumberStyles.Float, CultureInfo.InvariantCulture));
}
