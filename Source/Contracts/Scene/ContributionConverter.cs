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
    public static SceneContributionPoints.Contribution Convert(ScreenplaySyntax.ContributionSyntax contribution, string id)
    {
        var properties = new Dictionary<string, object?>();
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
            properties[SceneElementProperties.Destination] = CompositionConverter.Destination(contribution.Navigate);
        }

        var content = SceneElementFactory.Component(id, "core:contribution", properties);
        return new SceneContributionPoints.Contribution(contribution.ContributionPoint, content, contribution.Order);
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
    public static SceneContributionPoints.Contribution Convert(ScreenplaySyntax.ScreenContributionSyntax contribution, string id, BehaviorScope? behaviors)
    {
        var content = SceneElementFactory.Component(
            id,
            "core:contribution",
            slots: new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>>(StringComparer.Ordinal)
            {
                [ContentSlot] = ScreenDirectiveConverter.Convert(contribution.Directives, id, behaviors)
            });
        return new SceneContributionPoints.Contribution(contribution.ContributionPoint, content, contribution.Order);
    }
}
