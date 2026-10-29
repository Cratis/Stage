// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_degrading_an_owner;

public class with_an_unhandled_event_member_and_an_independent_slice : given.an_unsupported_event_request
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_render_the_independent_slice_in_a_degraded_plan() => Assert.True(_plan.Outcome == ArtifactRenderOutcome.Degraded,
        $"Outcome: {_plan.Outcome}; diagnostics: {string.Join(" | ", _plan.Diagnostics.Select(diagnostic => $"{diagnostic.Code} ({diagnostic.Artifact}): {diagnostic.Message}"))}");
    [Fact] void should_emit_the_valid_independent_command() => _plan.Artifacts.Any(artifact => artifact.Sources.Contains(_independentCommand)).ShouldBeTrue();
    [Fact] void should_not_emit_the_event_or_its_consuming_slices() => _plan.Artifacts.Where(artifact =>
        artifact.Sources.Contains(_unsupportedEvent) || artifact.Sources.Contains(_registerProject.Id) || artifact.Sources.Contains(_projectLookup.Id)).ShouldBeEmpty();
    [Fact] void should_report_the_root_event() => _plan.Refusals.Any(refusal => refusal.Artifact == _unsupportedEvent && refusal.Kind == "event" && refusal.Codes.Contains("STAGE-DEGRADE-001")).ShouldBeTrue();
    [Fact] void should_keep_the_original_root_message() => _plan.Degradations.Single(gap => gap.Diagnostic.Artifact == _unsupportedEvent).Diagnostic.Message.ShouldContain("STAGE-ESM-012");
    [Fact] void should_not_keep_errors_after_a_successful_retry() => _plan.Diagnostics.Where(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_mark_every_selected_omitted_owner_as_graph_proven() => _plan.Inventory!.Constructs.Where(construct => _plan.Refusals.Any(refusal => refusal.Artifact == construct.Identity))
        .All(construct => construct.Outcome == ArtifactRenderOutcome.Degraded && construct.Degradations.Single().PlaceholderKind == ArtifactRenderPlaceholderKind.GraphProvenOmittedLeaf).ShouldBeTrue();
    [Fact] void should_link_each_dependent_to_the_root() => _plan.Inventory!.Constructs.SelectMany(construct => construct.Degradations)
        .Where(gap => gap.Evidence.Diagnostic.Code == "STAGE-DEGRADE-005").All(gap => gap.Roots.SequenceEqual([new ArtifactRenderGapReference(_unsupportedEvent, "STAGE-DEGRADE-001")])).ShouldBeTrue();
}
