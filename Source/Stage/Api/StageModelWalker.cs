// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts;

namespace Cratis.Stage.Api;

/// <summary>
/// Represents one slice located within the event model, carrying the route location and the unique type
/// namespace the engine uses to build its API surface by convention.
/// </summary>
/// <param name="Slice">The slice.</param>
/// <param name="Location">The route location segments (used to build the conventional URL).</param>
/// <param name="TypeNamespace">A namespace unique to the slice, used for emitted command and read model types.</param>
public record LocatedSlice(Slice Slice, IReadOnlyList<string> Location, string TypeNamespace);

/// <summary>
/// Walks an <see cref="EventModel"/> and yields every slice within it, including those in nested sub-features.
/// </summary>
public static class StageModelWalker
{
    const string RootSegment = "Stage";

    /// <summary>
    /// Enumerates every slice in the model with its route location and unique type namespace.
    /// </summary>
    /// <param name="model">The event model to walk.</param>
    /// <returns>The located slices.</returns>
    public static IEnumerable<LocatedSlice> Slices(EventModel model)
    {
        foreach (var collection in model.Collections)
        {
            foreach (var module in collection.Modules)
            {
                var moduleSegment = ModelNaming.ToIdentifier(module.Name);
                foreach (var feature in module.Features)
                {
                    foreach (var located in WalkFeature(feature, [moduleSegment]))
                    {
                        yield return located;
                    }
                }
            }
        }
    }

    static IEnumerable<LocatedSlice> WalkFeature(Feature feature, IReadOnlyList<string> path)
    {
        var featurePath = new List<string>(path) { ModelNaming.ToIdentifier(feature.Name) };
        var location = new List<string> { RootSegment };
        location.AddRange(featurePath);

        foreach (var slice in feature.Slices)
        {
            var sliceSegment = ModelNaming.ToIdentifier(slice.Name);
            var typeNamespace = $"{RootSegment}.{string.Join('.', featurePath)}.{sliceSegment}";
            yield return new LocatedSlice(slice, location, typeNamespace);
        }

        foreach (var subFeature in feature.SubFeatures)
        {
            foreach (var located in WalkFeature(subFeature, featurePath))
            {
                yield return located;
            }
        }
    }
}
