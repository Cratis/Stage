// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.when_comparing_version_profiles;

public class and_semantic_payload_changes : Specification
{
    [Fact]
    void should_preserve_generation_options()
    {
        var model = SemanticModelSerializer.Deserialize(GeneratedValuesCorpus.V15.EsmBytes.AsSpan());
        var application = model.Application with
        {
            Concepts = [.. model.Application.Concepts.Select(concept => concept.Name == "UserId" ? concept with { Generation = concept.Generation! with { Prefix = "other" } } : concept)]
        };
        var changed = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application);
        round_trip_model_comparison.Compare(model, changed).IsEquivalent.ShouldBeFalse();
    }

    [Fact]
    void should_preserve_response_field_order()
    {
        var model = comparison_profile_models.Golden(7);
        var application = comparison_profile_models.Commands(model.Application, command => command.Response is SemanticRecordCommandResponse response
            ? command with { Response = response with { Fields = [.. response.Fields.Reverse()] } } : command);
        var changed = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application);
        round_trip_model_comparison.Compare(model, changed).IsEquivalent.ShouldBeFalse();
    }

    [Fact]
    void should_preserve_clock_offsets()
    {
        var model = SemanticModelSerializer.Deserialize(ClockOperandsCorpus.V37.EsmBytes.AsSpan());
        var application = comparison_profile_models.Commands(model.Application, command => command with
        {
            Requirements = [.. command.Requirements.Select(requirement => requirement.Condition is SemanticComparison { Right.Clock: { } clock } comparison
                ? requirement with { Condition = comparison with { Right = comparison.Right with { Clock = clock with { Amount = clock.Amount + 1 } } } } : requirement)]
        });
        var changed = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application);
        round_trip_model_comparison.Compare(model, changed).IsEquivalent.ShouldBeFalse();
    }
}
#endif
