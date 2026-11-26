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

public class and_a_legacy_destination_is_inferred : Specification
{
    [Fact]
    void should_handle_a_v31_event_shapes_style_implicit_destination()
    {
        var original = comparison_profile_models.Golden(31);
        var implicitApplication = comparison_profile_models.Commands(original.Application, command => command with { Destination = null });
        var legacy = ExecutableSemanticModel.Create(original.LanguageVersion, original.SemanticVersion, implicitApplication);
        var explicitDestination = comparison_profile_models.Calendar(original);

        Assert.Contains(comparison_profile_models.Commands(original), command => command.Destination is not null);
        round_trip_model_comparison.Compare(legacy, explicitDestination).IsEquivalent.ShouldBeTrue();
    }

    [Fact]
    void should_preserve_the_generated_identifier_null_production_exception()
    {
        var original = comparison_profile_models.Calendar(SemanticModelSerializer.Deserialize(RegisterProjectCorpus.V7.EsmBytes.AsSpan()));
        var application = comparison_profile_models.Commands(original.Application, command => command with
        {
            Destination = null,
            Produces = [.. command.Produces.Select(produced => produced with { Destination = null })]
        });
        var generated = ExecutableSemanticModel.Create(original.LanguageVersion, original.SemanticVersion, application);
        var converted = round_trip_model_profile.Convert(generated);

        Assert.Contains(comparison_profile_models.Commands(generated).SelectMany(command => command.Properties), property => property.IsIdentifier && property.IsGenerated);
        Assert.All(comparison_profile_models.Commands(converted), command => Assert.Null(command.Destination));
        round_trip_model_comparison.Compare(generated, comparison_profile_models.Calendar(original)).IsEquivalent.ShouldBeFalse();
    }
}
#endif
