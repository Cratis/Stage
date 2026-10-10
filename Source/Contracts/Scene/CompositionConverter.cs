// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using SceneCommon = Cratis.Scene.Model.Common;
using SceneElements = Cratis.Scene.Model.Elements;
using SceneExposure = Cratis.Scene.Model.Exposure;
using SceneScreens = Cratis.Scene.Model.Screens;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Translates Screenplay's typed composition declarations - exposures, instance values, template slot content,
/// template picker metadata and navigation destinations - into the Scene model types that carry the same meaning.
/// </summary>
/// <remarks>
/// Each construct maps onto its own Scene type rather than into an element's open property bag, so a Scene engine,
/// designer or renderer reads it the way it reads any other Scene application and nothing depends on a convention
/// only Stage knows.
/// </remarks>
public static class CompositionConverter
{
    const string NoScope = "none";

    /// <summary>
    /// Converts an <c language="csharp">exposure for</c> declaration.
    /// </summary>
    /// <param name="exposure">The exposure syntax.</param>
    /// <returns>The Scene exposure declaration.</returns>
    public static SceneExposure.ExposureDeclaration Exposure(ScreenplaySyntax.ExposureSyntax exposure) =>
        new(exposure.Owner, [.. exposure.Properties.Select(ExposedProperty)]);

    /// <summary>
    /// Converts an <c language="csharp">instance</c> declaration into one contribution per stored value or collection.
    /// </summary>
    /// <param name="instance">The instance syntax.</param>
    /// <returns>The Scene instance contributions, in declaration order.</returns>
    public static IEnumerable<SceneExposure.InstanceContribution> Instance(ScreenplaySyntax.InstanceContributionsSyntax instance) =>
        instance.Contributions.Select(contribution => new SceneExposure.InstanceContribution(
            instance.Instance,
            contribution.Component,
            contribution.Path,
            contribution.Value is null ? null : Json(contribution.Value),
            [.. contribution.Items.Select(ContributedItem)]));

    /// <summary>
    /// Converts template picker metadata: the template type, its category and the scopes it may be used at.
    /// </summary>
    /// <param name="templateType">The authored template type.</param>
    /// <param name="category">The authored category.</param>
    /// <param name="restrictsScopes">Whether the template declares <c language="csharp">scopes</c> at all.</param>
    /// <param name="scopes">The declared scopes; <c language="csharp">none</c> restricts the template to no scope.</param>
    /// <returns>The Scene template metadata.</returns>
    /// <remarks>
    /// A template that declares no <c language="csharp">scopes</c> may be used anywhere, which Scene expresses with no scope list
    /// at all. <c language="csharp">scopes none</c> is a restriction to nothing, an empty list, and is kept distinct from that.
    /// </remarks>
    public static SceneScreens.TemplateMetadata Metadata(string? templateType, string? category, bool restrictsScopes, IEnumerable<string> scopes) =>
        new(templateType, category, restrictsScopes ? [.. scopes.Where(scope => !string.Equals(scope, NoScope, StringComparison.Ordinal)).Select(Scope)] : null);

    /// <summary>
    /// Converts the content a template provides for its own slots.
    /// </summary>
    /// <param name="owner">The template name, used to derive unique element ids.</param>
    /// <param name="content">The template slot content syntax.</param>
    /// <param name="behaviors">What a <c language="csharp">uses</c> clause inside the content resolves against.</param>
    /// <returns>The content by slot name, or <see langword="null"/> when the template provides none.</returns>
    public static IReadOnlyDictionary<string, IReadOnlyList<SceneElements.SceneElement>>? Content(
        string owner,
        IEnumerable<ScreenplaySyntax.TemplateSlotContentSyntax> content,
        BehaviorScope? behaviors = null)
    {
        var slots = content.ToList();
        return slots.Count == 0
            ? null
            : slots.ToDictionary(
                slot => slot.Slot,
                slot => ScreenDirectiveConverter.Convert(slot.Directives, $"{owner}.content.{slot.Slot}", behaviors),
                StringComparer.Ordinal);
    }

    /// <summary>
    /// Converts a navigation directive into the Scene destination it names.
    /// </summary>
    /// <param name="navigate">The navigation syntax.</param>
    /// <returns>The Scene destination: target screen, route override, outlet and route parameter bindings.</returns>
    public static SceneCommon.DestinationReference Destination(ScreenplaySyntax.ScreenNavigateSyntax navigate) =>
        new(
            Screen: navigate.Screen,
            Outlet: navigate.Outlet,
            Route: navigate.Route,
            Kind: navigate.Outlet is null ? null : SceneCommon.DestinationKind.Outlet,
            RouteParameterBindings: RouteParameters(navigate));

    static Dictionary<string, SceneCommon.BindingExpression> RouteParameters(ScreenplaySyntax.ScreenNavigateSyntax navigate)
    {
        var parameters = navigate.Parameters.ToDictionary(
            parameter => parameter.Name,
            parameter => ScreenplayRichSyntax.Binding(parameter.Binding),
            StringComparer.Ordinal);

        // `navigate to X by id` names the parameter and reads it from the data the directive sits in.
        if (navigate.By is { } by && !parameters.ContainsKey(by))
        {
            parameters[by] = new(by);
        }

        return parameters;
    }

    static SceneExposure.ExposedProperty ExposedProperty(ScreenplaySyntax.ExposedPropertySyntax property) =>
        new(
            property.Component,
            property.Path,
            property.Label,
            property.IsCollection ? [.. property.Operations.Select(Operation)] : null,
            property.RestrictsFields ? [.. property.EditableFields] : null,
            property.ReExposes);

    static SceneExposure.ContributedItem ContributedItem(ScreenplaySyntax.ContributedItemSyntax item) =>
        new(item.Id, item.Values.ToDictionary(value => value.Field, value => Json(value.Value), StringComparer.Ordinal));

    static JsonElement Json(ScreenplaySyntax.ExpressionSyntax expression) =>
        JsonSerializer.SerializeToElement(ScreenplayRichSyntax.Value(expression));

    static SceneExposure.CollectionOperation Operation(string operation) => operation switch
    {
        "add" => SceneExposure.CollectionOperation.Add,
        "remove" => SceneExposure.CollectionOperation.Remove,
        "reorder" => SceneExposure.CollectionOperation.Reorder,
        "edit-fields" => SceneExposure.CollectionOperation.EditFields,
        _ => throw new UnknownCompositionValue("collection operation", operation),
    };

    static SceneScreens.TemplateScope Scope(string scope) => scope switch
    {
        "application" => SceneScreens.TemplateScope.Application,
        "module" => SceneScreens.TemplateScope.Module,
        "feature" => SceneScreens.TemplateScope.Feature,
        "subfeature" => SceneScreens.TemplateScope.Subfeature,
        "slice" => SceneScreens.TemplateScope.Slice,
        _ => throw new UnknownCompositionValue("template scope", scope),
    };
}
