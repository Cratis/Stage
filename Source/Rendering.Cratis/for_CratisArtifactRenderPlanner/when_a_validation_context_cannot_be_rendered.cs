// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_a_validation_context_cannot_be_rendered : a_render_with_an_invalid_typed_context
{
    void Establish() => Prepare(validation_body_model.Command("return context.Value > 0;"));
    void Because() => _plan = opaque_policy_model.Plan(_loaded);

    [Fact] void should_record_the_owner_gap() => _plan.ImplementationGaps.Any(gap => gap.Kind == ArtifactImplementationGapKind.Rendering && string.Equals(gap.Owner, _owner.ToString(), StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_preserve_the_existing_code_and_message() => rendering_refusals.Warning(_plan, "STAGE-ESM-021", _owner, "Validation 'Positive' has no unique wrapper-ready typed context with matching members and sources.").Artifact.ShouldEqual(_owner);
    [Fact] void should_omit_the_rejected_owner() => _plan.Artifacts.Any(artifact => artifact.Sources.Contains(_owner)).ShouldBeFalse();
    [Fact] void should_keep_independent_output() => _plan.Artifacts.Any(artifact => artifact.RelativePath.EndsWith("/Ping.cs", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_be_partial() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Partial);
    [Fact] void should_compile_the_remaining_output() => opaque_policy_model.Compile(_plan).GetTypes().Any(type => string.Equals(type.Name, "Ping", StringComparison.Ordinal)).ShouldBeTrue();
}
#endif
