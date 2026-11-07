// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Specifications;

/// <summary>
/// Verifies the specifications that seed a graph, append events and assert the graph the graph projections leave behind.
/// </summary>
/// <remarks>
/// A graph specification asserts the graph, not a slice's command or read model, so it is decided by Screenplay's reference graph
/// runner over the same plan the slice specifications run against: an in-memory property graph that applies the five operations of a graph
/// projection. It is selected with the slice that owns it, or with the application scope (or no scope at all).
/// </remarks>
static class SemanticGraphSpecifications
{
    internal const string GraphKind = "Graph";

    internal static IEnumerable<SemanticSpecificationRunRecord> Run(SemanticExecutionPlan plan, SemanticSpecificationSelection selection)
    {
        var application = plan.Model.Application;
        if (application.GraphSpecifications.IsDefaultOrEmpty) return [];

        var scopes = selection.Scopes.IsDefault ? [] : selection.Scopes;
        var slices = SemanticVersionFeatures.Slices(application).ToLookup(slice => slice.Name, StringComparer.Ordinal);
        var selected = application.Modules.SelectMany(module => SelectedSlices(module.Features, scopes, scopes.IsEmpty || scopes.Contains(application.Id) || scopes.Contains(module.Id))).ToHashSet();
        bool Selected(string owner) => slices[owner].Any(slice => selected.Contains(slice.Id));

        return SemanticGraphRunner.RunAll(plan.Model).Where(result => Selected(result.Owner)).Select(result => new SemanticSpecificationRunRecord(
            $"{application.Id}/{GraphKind}/{result.Owner}/{result.Name}",
            result.Name,
            slices[result.Owner].Select(slice => slice.Id.ToString()).FirstOrDefault() ?? $"{application.Id}/{result.Owner}",
            GraphKind,
            result.Passed ? SemanticSpecificationOutcome.Passed : SemanticSpecificationOutcome.Failed,
            null,
            null,
            result.Failures,
            null));
    }

    // A scope selects what it contains: the application, a module or a feature selects every slice below it, a slice selects itself.
    static IEnumerable<SemanticId> SelectedSlices(IEnumerable<SemanticFeature> features, System.Collections.Immutable.ImmutableArray<SemanticId> scopes, bool inherited)
    {
        foreach (var feature in features)
        {
            var selected = inherited || scopes.Contains(feature.Id);
            foreach (var slice in feature.Slices.Where(slice => selected || scopes.Contains(slice.Id))) yield return slice.Id;
            foreach (var id in SelectedSlices(feature.Features, scopes, selected)) yield return id;
        }
    }
}
