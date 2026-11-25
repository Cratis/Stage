// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.when_comparing_version_profiles;

public class and_a_version_gate_changes_meaning : Specification
{
    [Fact]
    void should_not_equate_v7_whole_numbers_with_the_same_graph_at_v8_or_later()
    {
        var source = meaning_gate_models.Response("Int", "1");
        source.SemanticVersion.ShouldEqual(SemanticVersion.V7);
        RejectComparison(source, "V8 WholeNumber");
    }

    [Fact]
    void should_find_whole_numbers_in_an_unreferenced_concept()
    {
        var source = meaning_gate_models.Response();
        var concept = meaning_gate_models.Generated.Application.Concepts.Single() with { Primitive = SemanticPrimitiveType.WholeNumber };
        var application = source.Application with { Concepts = [concept] };
        var model = ExecutableSemanticModel.Create(source.LanguageVersion, source.SemanticVersion, application);
        RejectComparison(model, "V8 WholeNumber");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    void should_not_change_single_event_or_greedy_any_order_matching(int count)
    {
        var source = meaning_gate_models.Specifications(meaning_gate_models.Response(), specification => specification with
        {
            ThenEventsInAnyOrder = true,
            ThenEvents = [.. Enumerable.Repeat(specification.ThenEvents[0], count)]
        });
        RejectComparison(source, "V8 unordered event matching");
    }

    [Fact] void should_not_change_v1_fact_identity_or_context() => RejectComparison(comparison_profile_models.Legacy, "V1 occurrence identity/context");
    [Fact] void should_not_change_pre_v6_action_execution_phases() => RejectComparison(meaning_gate_models.Action, "V6 execution phases");
    [Fact] void should_not_guess_pre_v11_numeric_literal_lowering() => RejectComparison(meaning_gate_models.WithoutEvents(meaning_gate_models.Response("Decimal", "1.25")), "V11 numeric literal lowering");
    [Fact] void should_not_change_pre_v8_admission_of_external_fact_routes() => RejectComparison(meaning_gate_models.Response(), "V8 occurrence route admission");
    [Fact] void should_not_strengthen_legacy_generation_checks() => RejectComparison(meaning_gate_models.Generated, "V15 generated-value checks");

    [Fact]
    void should_not_reinterpret_a_legacy_freshness_error()
    {
        var source = meaning_gate_models.Specifications(meaning_gate_models.Response(), specification => specification with
        {
            ThenEvents = [],
            ThenErrors = [new SemanticSpecificationError(null, "A generated event source already has facts; retry the command with a fresh generated value.")]
        });
        RejectComparison(meaning_gate_models.WithoutEvents(source), "V15 generated-value checks");
    }

    [Theory]
    [InlineData(32)]
    [InlineData(33)]
    void should_not_change_typed_capture_identity_admission_or_rendering(int version) => RejectComparison(comparison_profile_models.Golden(version), "V32/V33 capture identity");

    [Fact]
    void should_keep_payload_losses_beside_the_version_meaning_loss()
    {
        var source = meaning_gate_models.Response("Int", "1");
        var modern = comparison_profile_models.Calendar(source);
        var application = comparison_profile_models.Commands(modern.Application, command => command with { Name = "RenamedDeposit" });
        var changed = ExecutableSemanticModel.Create(modern.LanguageVersion, modern.SemanticVersion, application);
        var comparison = round_trip_model_comparison.Compare(source, changed);

        comparison.IsEquivalent.ShouldBeFalse();
        Assert.Single(comparison.Differences.Where(difference => difference.Path.StartsWith("/version-meaning-unproven[", StringComparison.Ordinal)));
        Assert.Contains(comparison.Differences, difference => difference.Path.StartsWith("/application/", StringComparison.Ordinal));
    }

    [Fact]
    void should_retain_the_graph_path_in_the_conversion_failure()
    {
        var exception = Assert.Throws<InvalidSemanticContract>(() => round_trip_model_profile.Convert(meaning_gate_models.Response("Int", "1")));
        Assert.Contains("/application/", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Primitive", exception.Message, StringComparison.Ordinal);
    }

    static void RejectComparison(ExecutableSemanticModel source, string gate)
    {
        var refusal = Assert.Throws<InvalidSemanticContract>(() => round_trip_model_profile.Convert(source));
        Assert.Contains(gate, refusal.Message, StringComparison.Ordinal);
        var modern = comparison_profile_models.Calendar(source);
        var comparison = round_trip_model_comparison.Compare(source, modern);

        comparison.IsEquivalent.ShouldBeFalse();
        Assert.Contains(comparison.Differences, difference => difference.Path.StartsWith("/version-meaning-unproven[", StringComparison.Ordinal) && difference.Path.Contains(gate, StringComparison.Ordinal));
    }
}
#endif
