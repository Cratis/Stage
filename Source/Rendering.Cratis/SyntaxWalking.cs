// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Extension methods for walking the Screenplay syntax tree.
/// </summary>
public static class SyntaxWalking
{
    /// <summary>
    /// Gets a feature and every sub-feature nested underneath it, recursively.
    /// </summary>
    /// <param name="feature">The feature to walk.</param>
    /// <returns>The feature and its nested sub-features.</returns>
    public static IEnumerable<FeatureSyntax> AllFeatures(this FeatureSyntax feature) =>
        new[] { feature }.Concat(feature.Features.SelectMany(AllFeatures));

    /// <summary>
    /// Gets every feature and sub-feature declared in a module, recursively.
    /// </summary>
    /// <param name="module">The module to walk.</param>
    /// <returns>Every feature and sub-feature in the module.</returns>
    public static IEnumerable<FeatureSyntax> AllFeatures(this ModuleSyntax module) =>
        module.Features.SelectMany(AllFeatures);

    /// <summary>
    /// Gets every slice declared anywhere in an application, across all modules and (sub-)features.
    /// </summary>
    /// <param name="application">The application to walk.</param>
    /// <returns>Every slice in the application.</returns>
    public static IEnumerable<SliceSyntax> AllSlices(this ApplicationSyntax application) =>
        application.Modules.SelectMany(module => module.AllFeatures()).SelectMany(feature => feature.Slices);
}
