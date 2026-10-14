// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationArtifactRenderer.given;

public static class previous_slice_sources
{
    public static ImmutableArray<SemanticId> Of(PlannedArtifact artifact, ExecutableSemanticModel model)
    {
        var owners = model.Application.Modules.SelectMany(Slices).Where(slice =>
            slice.Specifications.Any(specification => artifact.Sources.Contains(specification.Id)) ||
            slice.Reducers.Any(reducer => artifact.Sources.Contains(reducer.ReadModel) &&
                artifact.RelativePath.EndsWith($"/{Naming.Identifiers.ToPascalCase(reducer.Name)}.cs", StringComparison.Ordinal)))
            .Select(slice => slice.Id).ToHashSet();

        return [.. artifact.Sources.Where(source => !owners.Contains(source))];
    }

    static IEnumerable<SemanticSlice> Slices(SemanticFeature feature) =>
        feature.Slices.Concat(feature.Features.SelectMany(Slices));

    static IEnumerable<SemanticSlice> Slices(SemanticModule module) => module.Features.SelectMany(Slices);
}
