// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_guarded_freshness : Specification
{
    ArtifactRenderPlan _plan = null!;
    string _command = string.Empty;
    string _rejection = string.Empty;

    void Because()
    {
        _plan = a_generation_model.Plan();
        _command = Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath == "Billing/Creation/Start/Start.cs").Bytes.AsSpan());
        _rejection = Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("when_reused_source.cs", StringComparison.Ordinal)).Bytes.AsSpan());
    }

    [Fact] void should_admit_the_v15_strategies() => _plan.Success.ShouldBeTrue();
    [Fact] void should_return_events_with_atomic_scopes() => _command.ShouldContain("EventsWithConcurrencyScopes");
    [Fact] void should_scope_several_events_on_one_source_only_once() => _command.Split("EventSequenceNumber.BeforeFirst", StringSplitOptions.None).Length.ShouldEqual(2);
    [Fact] void should_preserve_the_response_tuple() => _command.ShouldContain("(global::GenerationApp.Common.Receipt, global::Cratis.Chronicle.EventSequences.EventsWithConcurrencyScopes)");
    [Fact] void should_assert_the_arc_concurrency_reason() => _rejection.ShouldContain("ValidationResultReason.ConcurrencyViolation");
    [Fact] void should_seed_the_prior_event_in_the_log() => _rejection.ShouldContain("await _scenario.EventScenario.Given");
}
