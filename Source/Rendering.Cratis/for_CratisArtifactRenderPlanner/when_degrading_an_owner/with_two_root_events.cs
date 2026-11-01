// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_degrading_an_owner;

public class with_two_root_events : given.an_unsupported_event_request
{
    ArtifactRenderPlan _first = null!;
    ArtifactRenderPlan _reordered = null!;

    void Establish() => UnsupportedProperties(_model.Application.Modules.Single(module => module.Name == "Billing").Features.Single().Slices.Single().Events.Single());

    void Because()
    {
        _first = _planner.Plan(_request);
        var reordered = _model with { };
        typeof(ExecutableSemanticModel).GetField("<Application>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(reordered, _model.Application with { Modules = [.. _model.Application.Modules.Reverse()] });
        _reordered = _planner.Plan(_request with { Model = reordered });
    }

    [Fact] void should_degrade_both_traversal_orders() => (_first.Outcome == ArtifactRenderOutcome.Degraded && _reordered.Outcome == ArtifactRenderOutcome.Degraded).ShouldBeTrue();
    [Fact] void should_identify_both_root_events() => _first.Refusals.Count(refusal => refusal.Kind == "event" && refusal.Codes.Contains("STAGE-DEGRADE-001")).ShouldEqual(2);
    [Fact] void should_keep_artifact_bytes_including_reports_and_inventory() => Assert.Equal(_first.Artifacts.Select(artifact => (artifact.RelativePath, artifact.Sha256)), _reordered.Artifacts.Select(artifact => (artifact.RelativePath, artifact.Sha256)));
    [Fact] void should_keep_artifact_sources() => Assert.Equal(Sources(_first), Sources(_reordered));
    [Fact] void should_keep_degradations() => Assert.Equal(_first.Degradations.ToArray(), _reordered.Degradations.ToArray());
    [Fact] void should_keep_refusals() => Assert.Equal(Refusals(_first), Refusals(_reordered));
    [Fact] void should_keep_diagnostics() => Assert.Equal(_first.Diagnostics.ToArray(), _reordered.Diagnostics.ToArray());
    [Fact] void should_keep_the_digest() => _reordered.Digest.ShouldEqual(_first.Digest);

    static IEnumerable<string> Sources(ArtifactRenderPlan plan) => plan.Artifacts.Select(artifact => $"{artifact.RelativePath}: {string.Join(',', artifact.Sources)}");
    static IEnumerable<string> Refusals(ArtifactRenderPlan plan) => plan.Refusals.Select(refusal => $"{refusal.Artifact}: {refusal.Kind}: {string.Join(',', refusal.Codes)}");
}
