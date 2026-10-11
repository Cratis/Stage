// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneCommon = Cratis.Scene.Model.Common;
using SceneContributionPoints = Cratis.Scene.Model.ContributionPoints;
using SceneElements = Cratis.Scene.Model.Elements;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Converts a compiled Screenplay <see cref="ScreenplaySyntax.ContributionSyntax"/> into a
/// <see cref="SceneContributionPoints.Contribution"/> - part of Cratis/Stage#37.
/// </summary>
/// <remarks>
/// <see cref="SceneContributionPoints.Contribution"/> has no sibling <c language="csharp">Label</c>/<c language="csharp">Navigate</c> fields -
/// they fold into the <c language="csharp">Content</c> element's open <c language="csharp">Properties</c> bag as <c language="csharp">label</c>/<c language="csharp">targetScreen</c>/
/// <c language="csharp">routeParameterBindings</c>, matching the contract <c language="csharp">@cratis/scene.engine</c>'s <c language="csharp">extractNavigationItem</c>
/// already reads (Cratis/Scene#2) so a contribution built here is consumable by the built-in <c language="csharp">NavBar</c>
/// without either side changing.
/// </remarks>
public static class ContributionConverter
{
    /// <summary>
    /// The slot of a screen contribution's element that holds the contributed directives.
    /// </summary>
    public const string ContentSlot = "content";

    /// <summary>
    /// Converts a <see cref="ScreenplaySyntax.ContributionSyntax"/> into a <see cref="SceneContributionPoints.Contribution"/>.
    /// </summary>
    /// <param name="contribution">The <see cref="ScreenplaySyntax.ContributionSyntax"/> to convert.</param>
    /// <param name="id">The id for the contributed <c language="csharp">SceneElement</c>, unique within its screen.</param>
    /// <returns>The converted <see cref="SceneContributionPoints.Contribution"/>.</returns>
    public static SceneContributionPoints.Contribution Convert(ScreenplaySyntax.ContributionSyntax contribution, string id) =>
        Convert(contribution, id, []);

    /// <summary>
    /// Converts a <see cref="ScreenplaySyntax.ContributionSyntax"/> declared inside a module and its features.
    /// </summary>
    /// <param name="contribution">The <see cref="ScreenplaySyntax.ContributionSyntax"/> to convert.</param>
    /// <param name="id">The id for the contributed <c language="csharp">SceneElement</c>, unique within its screen.</param>
    /// <param name="hierarchy">The module and features, outermost first, the contribution was declared in.</param>
    /// <returns>The converted <see cref="SceneContributionPoints.Contribution"/>, carrying the hierarchy it was declared in.</returns>
    public static SceneContributionPoints.Contribution Convert(ScreenplaySyntax.ContributionSyntax contribution, string id, IReadOnlyList<string> hierarchy)
    {
        new UiSyntaxAdmission().VisitNode(contribution);
        var properties = new Dictionary<string, object?>();
        if (contribution.Group is not null)
        {
            properties["group"] = contribution.Group;
        }

        if (contribution.Label is not null)
        {
            properties["label"] = contribution.Label;
        }

        if (contribution.Navigate is not null)
        {
            properties["targetScreen"] = contribution.Navigate.Screen;
            properties["routeParameterBindings"] = contribution.Navigate.By is null
                ? []
                : new Dictionary<string, SceneCommon.BindingExpression> { [contribution.Navigate.By] = new(contribution.Navigate.By) };
        }

        // The rest of Scene's NavigationItem: the stable id, the icon, the presentation hint, the group and where it opens.
        // An item that names none of them carries none, so a plain entry is exactly what it was.
        if (contribution.Id is not null) properties["id"] = contribution.Id;
        if (contribution.Icon is not null) properties["icon"] = contribution.Icon;
        if (contribution.Presentation is not null) properties["presentation"] = contribution.Presentation;
        if (contribution.Group is not null) properties["group"] = contribution.Group;
        if (DestinationOf(contribution) is { } destination) properties[SceneElementProperties.Destination] = destination;

        if (hierarchy.Count > 0)
        {
            properties[SceneElementProperties.Hierarchy] = hierarchy.ToArray();
        }

        var content = SceneElementFactory.Component(id, "core:contribution", properties);
        return new SceneContributionPoints.Contribution(contribution.ContributionPoint, content, contribution.Order);
    }

    static SceneCommon.DestinationReference? DestinationOf(ScreenplaySyntax.ContributionSyntax contribution)
    {
        var navigate = contribution.Navigate is null ? null : CompositionConverter.Destination(contribution.Navigate);
        if (contribution.Destination is not { } destination)
        {
            return navigate;
        }

        // `destination outlet|dialog|external <target>` says where the entry opens; a `navigate to` beside it supplies the screen
        // and parameters, so the two compose instead of one hiding the other.
        var opens = navigate ?? new SceneCommon.DestinationReference(RouteParameterBindings: new Dictionary<string, SceneCommon.BindingExpression>());
        return destination.Kind switch
        {
            ScreenplaySyntax.ContributionDestinationKind.Outlet => opens with { Outlet = destination.Target, Kind = SceneCommon.DestinationKind.Outlet },
            ScreenplaySyntax.ContributionDestinationKind.Dialog => opens with { Dialog = destination.Target, Kind = SceneCommon.DestinationKind.Dialog },
            ScreenplaySyntax.ContributionDestinationKind.External => opens with { Route = destination.Target, Kind = SceneCommon.DestinationKind.External },
            _ => throw new UnknownCompositionValue("contribution destination", destination.Kind.ToString()),
        };
    }

    /// <summary>
    /// Converts a <c language="csharp">contribute to</c> block written inside a screen.
    /// </summary>
    /// <param name="contribution">The screen contribution syntax.</param>
    /// <param name="id">The id the contributed content element is given.</param>
    /// <param name="behaviors">What a <c language="csharp">uses</c> clause inside the contribution resolves against.</param>
    /// <returns>The Scene contribution, carrying the contributed directives in the content element's slot.</returns>
    /// <remarks>
    /// The contributed directives keep their own Scene elements; the contribution element only groups them, the way
    /// a feature-level contribution's element carries its navigation entry, so a contribution point receives one
    /// element per contribution whatever was written inside it.
    /// </remarks>
    public static SceneContributionPoints.Contribution Convert(ScreenplaySyntax.ScreenContributionSyntax contribution, string id, BehaviorScope? behaviors) =>
        Convert(contribution, id, behaviors, []);

    /// <summary>
    /// Converts a <c language="csharp">contribute to</c> block written inside a screen, recording where the screen sits.
    /// </summary>
    /// <param name="contribution">The screen contribution syntax.</param>
    /// <param name="id">The id the contributed content element is given.</param>
    /// <param name="behaviors">What a <c language="csharp">uses</c> clause inside the contribution resolves against.</param>
    /// <param name="hierarchy">The modules and features, outermost first, the screen was declared in.</param>
    /// <returns>The Scene contribution, carrying the hierarchy of the screen that contributed it.</returns>
    public static SceneContributionPoints.Contribution Convert(ScreenplaySyntax.ScreenContributionSyntax contribution, string id, BehaviorScope? behaviors, IReadOnlyList<string> hierarchy)
    {
        var content = SceneElementFactory.Component(
            id,
            "core:contribution",
            hierarchy.Count > 0 ? new Dictionary<string, object?> { [SceneElementProperties.Hierarchy] = hierarchy.ToArray() } : null,
            slots: new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>>(StringComparer.Ordinal)
            {
                [ContentSlot] = ScreenDirectiveConverter.Convert(contribution.Directives, id, behaviors)
            });
        return new SceneContributionPoints.Contribution(contribution.ContributionPoint, content, contribution.Order);
    }
}
