// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering.Model;

/// <summary>
/// Selects the slices of a semantic application that a render request's scopes cover, independent of any target.
/// </summary>
/// <remarks>
/// Ported from Cratis/Stage.Axon Source/Rendering.Axon/Sdk/ScopeSelection.cs at acc30c3c48e00a66db6f10453d00ed513f5a48df.
/// </remarks>
internal static class RenderScopeSelection
{
    /// <summary>
    /// Selects the slices covered by the request's scope and additional scopes, in declaration order and without duplicates.
    /// </summary>
    /// <param name="request">The render request.</param>
    /// <returns>The selected slices.</returns>
    internal static IReadOnlyList<LocatedSemanticSlice> Select(ArtifactRenderRequest request) =>
        [.. Locate(request.Model.Application).Where(slice => new[] { request.Scope }.Concat(request.AdditionalScopes).Any(scope => Matches(scope, slice)))];

    /// <summary>
    /// Determines whether the scope's artifact exists in the request's application.
    /// </summary>
    /// <param name="request">The render request.</param>
    /// <param name="scope">The scope to check.</param>
    /// <returns><see langword="true"/> when the scope's artifact exists.</returns>
    internal static bool Exists(ArtifactRenderRequest request, ArtifactRenderScope scope) => scope.Kind switch
    {
        ArtifactRenderScopeKind.Application => scope.Artifact == request.Model.Application.Id,
        ArtifactRenderScopeKind.Module => ModuleExists(request.Model.Application, scope),
        ArtifactRenderScopeKind.Feature => FeatureExists(request.Model.Application, scope),
        ArtifactRenderScopeKind.Slice => SliceExists(request.Model.Application, scope),
        _ => false
    };

    /// <summary>
    /// Locates every slice of an application with its containing module, features and placement path.
    /// </summary>
    /// <param name="application">The semantic application.</param>
    /// <returns>The located slices in declaration order: a feature's own slices, then its child features'.</returns>
    internal static IEnumerable<LocatedSemanticSlice> Locate(SemanticApplication application) =>
        application.Modules.SelectMany(module => module.Features.SelectMany(feature => Locate(module, feature, [], [module.Name])));

    /// <summary>
    /// Determines whether the application contains the scoped module.
    /// </summary>
    /// <param name="application">The semantic application.</param>
    /// <param name="scope">The module scope.</param>
    /// <returns>Whether the module exists.</returns>
    static bool ModuleExists(SemanticApplication application, ArtifactRenderScope scope) =>
        application.Modules.Any(module => module.Id == scope.Artifact);

    /// <summary>
    /// Determines whether the application contains the scoped feature at any depth.
    /// </summary>
    /// <param name="application">The semantic application.</param>
    /// <param name="scope">The feature scope.</param>
    /// <returns>Whether the feature exists.</returns>
    static bool FeatureExists(SemanticApplication application, ArtifactRenderScope scope) =>
        application.Modules.SelectMany(module => module.Features).SelectMany(Features).Any(feature => feature.Id == scope.Artifact);

    /// <summary>
    /// Determines whether the application contains the scoped slice.
    /// </summary>
    /// <param name="application">The semantic application.</param>
    /// <param name="scope">The slice scope.</param>
    /// <returns>Whether the slice exists.</returns>
    static bool SliceExists(SemanticApplication application, ArtifactRenderScope scope) =>
        Locate(application).Any(slice => slice.Slice.Id == scope.Artifact);

    /// <summary>
    /// Enumerates a feature and its descendants in declaration order.
    /// </summary>
    /// <param name="feature">The containing feature.</param>
    /// <returns>The feature and its descendants.</returns>
    static IEnumerable<SemanticFeature> Features(SemanticFeature feature) =>
        new[] { feature }.Concat(feature.Features.SelectMany(Features));

    /// <summary>
    /// Determines whether a scope covers a located slice.
    /// </summary>
    /// <param name="scope">The render scope.</param>
    /// <param name="slice">The located slice.</param>
    /// <returns>Whether the scope covers the slice.</returns>
    static bool Matches(ArtifactRenderScope scope, LocatedSemanticSlice slice) => scope.Kind switch
    {
        ArtifactRenderScopeKind.Application => true,
        ArtifactRenderScopeKind.Module => slice.Module.Id == scope.Artifact,
        ArtifactRenderScopeKind.Feature => slice.Features.Any(feature => feature.Id == scope.Artifact),
        ArtifactRenderScopeKind.Slice => slice.Slice.Id == scope.Artifact,
        _ => false
    };

    /// <summary>
    /// Locates a feature's slices before visiting its child features.
    /// </summary>
    /// <param name="module">The containing module.</param>
    /// <param name="feature">The current feature.</param>
    /// <param name="ancestors">The containing features, outermost first.</param>
    /// <param name="path">The path to the current feature's parent.</param>
    /// <returns>The located slices in declaration order.</returns>
    static IEnumerable<LocatedSemanticSlice> Locate(SemanticModule module, SemanticFeature feature, IReadOnlyList<SemanticFeature> ancestors, IReadOnlyList<string> path)
    {
        SemanticFeature[] features = [.. ancestors, feature];
        string[] featurePath = [.. path, feature.Name];
        return feature.Slices.Select(slice => new LocatedSemanticSlice(module, features, slice, [.. featurePath, slice.Name]))
            .Concat(feature.Features.SelectMany(child => Locate(module, child, features, featurePath)));
    }
}
