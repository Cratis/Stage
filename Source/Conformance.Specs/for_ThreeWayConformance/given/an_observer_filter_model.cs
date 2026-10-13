// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

internal static class an_observer_filter_model
{
    internal static ExecutableSemanticModel Create(string specification = "RoutedReaction")
    {
        var model = SemanticModelSerializer.Deserialize(EventRoutesCorpus.ObserverFiltersV10.EsmBytes.AsSpan());
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var example = slice.Specifications.Single(value => value.Name == specification);
        if (!example.ThenAbsentReadModels.IsEmpty)
        {
            // Absence has its own earlier 027 refusal. An event expectation isolates excluded-trigger 011.
            example = example with
            {
                ThenAbsentReadModels = [],
                ThenEvents = slice.Specifications.Single(value => value.Name == "RoutedReaction").ThenEvents
            };
        }
        var selected = slice with
        {
            Reducers = [],
            Commands = [.. slice.Commands.Where(command => command.Name != "Fold")],

            // This derived fixture covers reaction events only, not the canonical Followups assertion.
            ReadModels = [],
            Projections = [],
            Queries = [],
            Specifications = [example with { GivenReadModels = [], ThenReadModels = [], ThenQueries = [] }]
        };

        return ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [selected] }] }]
        });
    }
}
