// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneScreens = Cratis.Scene.Model.Screens;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Collects the template assignments and uses of a compiled Screenplay application as Scene <see cref="SceneScreens.TemplateReference"/>s.
/// </summary>
/// <remarks>
/// <c language="csharp">template &lt;Name&gt;</c> is an own template; <c language="csharp">template blueprint &lt;Blueprint&gt; &lt;Name&gt; [version "&lt;range&gt;"]</c> is a template a blueprint
/// package ships, and its version is the semver range Scene's <c language="csharp">resolveTemplateReference</c> checks against the installed blueprint.
/// The category of a template travels as <see cref="SceneScreens.TemplateMetadata.Category"/> and is not part of the reference.
/// </remarks>
public static class TemplateReferenceConverter
{
    /// <summary>
    /// Collects every template reference of an application, in declaration order.
    /// </summary>
    /// <param name="application">The compiled <see cref="ScreenplaySyntax.ApplicationSyntax"/>.</param>
    /// <returns>The <see cref="TemplateAssignment"/>s.</returns>
    public static IReadOnlyList<TemplateAssignment> Collect(ScreenplaySyntax.ApplicationSyntax application)
    {
        var result = new List<TemplateAssignment>();
        result.AddRange(application.Templates.Select(assignment => Assignment("application", string.Empty, assignment)));
        foreach (var module in application.Modules)
        {
            result.AddRange(module.Templates.Select(assignment => Assignment("module", module.Name, assignment)));
            foreach (var feature in module.Features)
            {
                Collect(feature, module.Name, result);
            }
        }

        return result;
    }

    static void Collect(ScreenplaySyntax.FeatureSyntax feature, string parent, List<TemplateAssignment> result)
    {
        var path = $"{parent}.{feature.Name}";
        result.AddRange(feature.Templates.Select(assignment => Assignment("feature", path, assignment)));
        foreach (var slice in feature.Slices)
        {
            var slicePath = $"{path}.{slice.Name}";
            result.AddRange(slice.Templates.Select(assignment => Assignment("slice", slicePath, assignment)));
            foreach (var screen in slice.Screens)
            {
                result.AddRange(screen.Directives.OfType<ScreenplaySyntax.ScreenTemplateReferenceSyntax>()
                    .Select(reference => new TemplateAssignment("screen", $"{slicePath}.{screen.Name}", Reference(reference.Name, reference.Blueprint, reference.Version))));
            }
        }

        foreach (var nested in feature.Features)
        {
            Collect(nested, path, result);
        }
    }

    static TemplateAssignment Assignment(string scope, string owner, ScreenplaySyntax.TemplateAssignmentSyntax assignment) =>
        new(scope, owner, Reference(assignment.Name, assignment.Blueprint, assignment.Version));

    internal static SceneScreens.TemplateReference Reference(string name, string? blueprint, string? version) =>
        blueprint is null
            ? new(SceneScreens.TemplateReferenceSource.Own, name)
            : new(SceneScreens.TemplateReferenceSource.Blueprint, name, blueprint, version);
}
