// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Api;

namespace Cratis.Stage.Semantics;

internal sealed record LocatedSemanticSlice(SemanticSlice Slice, IReadOnlyList<string> Location, string TypeNamespace)
{
    internal IReadOnlyList<string> CanonicalLocation => [.. Location, ModelNaming.ToIdentifier(Slice.Name)];
}

internal static class SemanticModelWalker
{
    internal static IEnumerable<LocatedSemanticSlice> Slices(ExecutableSemanticModel model) =>
        model.Application.Modules.SelectMany(module => module.Features.SelectMany(feature => Walk(feature, [ModelNaming.ToIdentifier(module.Name)])));

    static IEnumerable<LocatedSemanticSlice> Walk(SemanticFeature feature, IReadOnlyList<string> parent)
    {
        var path = new List<string>(parent) { ModelNaming.ToIdentifier(feature.Name) };
        foreach (var slice in feature.Slices)
        {
            yield return new(slice, ["Stage", .. path], $"Stage.{string.Join('.', path)}.{ModelNaming.ToIdentifier(slice.Name)}");
        }

        foreach (var nested in feature.Features)
        {
            foreach (var slice in Walk(nested, path))
            {
                yield return slice;
            }
        }
    }
}
