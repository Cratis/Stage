// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;

internal static class pattern_random_fixtures_corpus
{
    internal static ExecutableSemanticModel V21
    {
        get
        {
            var model = SemanticModelSerializer.Deserialize(GenerationFixturesCorpus.V21.EsmBytes.AsSpan());
            var application = model.Application with
            {
                Concepts = [.. model.Application.Concepts.Where(concept => concept.Generation?.Kind != "implementation")],
                Modules = [.. model.Application.Modules.Select(module => module with
                {
                    Features = [.. module.Features.Select(feature => feature with
                    {
                        Slices = [.. feature.Slices.Select(slice => slice with
                        {
                            Commands = [.. slice.Commands.Where(command => command.Name != "TenantValue")],
                            Specifications = [.. slice.Specifications.Where(specification => Admitted.Contains(specification.Name, StringComparer.Ordinal))]
                        })]
                    })]
                })]
            };

            return ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application);
        }
    }

    // Invalid pins are checked separately: the renderer refuses them before emitting an application.
    internal static readonly string[] Admitted = ["UtcLeapDay", "WidthOnlyWithoutClock", "ChecksumShapeOnly", "NumericFixture", "FreshGeneratedSource", "GuardedSourceReuse"];
}
#endif
