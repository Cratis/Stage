// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis;

static class SemanticExclusionComments
{
    internal static IEnumerable<PlannedArtifact> For(ArtifactRenderRequest request)
    {
        if (request.Exclusions.IsDefaultOrEmpty) yield break;
        var exclusions = ArtifactRenderPlan.Create(request, [], []).Exclusions;
        var application = request.Model.Application;
        var parents = Parents(application, DomainPlacementInput.From(request.Profile)).ToDictionary(parent => parent.Id);
        foreach (var group in exclusions.GroupBy(exclusion => exclusion.ParentId is { IsSet: true } id ? id : application.Id))
        {
            if (!parents.TryGetValue(group.Key, out var parent))
                throw new InvalidArtifactRenderContract("A semantic exclusion parent must be a retained application, module, or feature.");
            var lines = group.OrderBy(exclusion => exclusion.Kind.ToString(), StringComparer.Ordinal).ThenBy(exclusion => exclusion.Address, StringComparer.Ordinal).Select(Comment);
            var path = string.Join('/', parent.Path.Select(Identifiers.ToPascalCase).Append("ScreenplayExclusions.g.cs"));
            yield return PlannedArtifact.CreateText(path, string.Join('\n', lines) + "\n", [parent.Id]);
        }
    }

    static string Comment(SemanticExclusion exclusion)
    {
        var codes = exclusion.Diagnostics.Select(diagnostic => diagnostic.Code).Distinct().Order(StringComparer.Ordinal);
        return $"// Differs from Screenplay: {SingleLine(exclusion.Address)} excluded ({SingleLine(string.Join(", ", codes))})";
    }

    static string SingleLine(string text) => new([.. text.Select(character => char.IsControl(character) || character is '\u2028' or '\u2029' ? ' ' : character)]);

    static IEnumerable<Parent> Parents(SemanticApplication application, ImmutableArray<string> domain)
    {
        yield return new(application.Id, []);
        foreach (var module in application.Modules)
        {
            var path = domain.Add(module.Name);
            yield return new(module.Id, path);
            foreach (var parent in module.Features.SelectMany(feature => FeatureParents(feature, path))) yield return parent;
        }
    }

    static IEnumerable<Parent> FeatureParents(SemanticFeature feature, ImmutableArray<string> path)
    {
        var current = path.Add(feature.Name);
        yield return new(feature.Id, current);
        foreach (var parent in feature.Features.SelectMany(nested => FeatureParents(nested, current))) yield return parent;
    }

    sealed record Parent(SemanticId Id, ImmutableArray<string> Path);
}
