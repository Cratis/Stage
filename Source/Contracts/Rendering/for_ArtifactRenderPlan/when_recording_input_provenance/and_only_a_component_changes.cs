// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan.when_recording_input_provenance;

public class and_only_a_component_changes : given.a_provenance_request
{
    ArtifactRenderPlan _original = null!;
    ArtifactRenderPlan _changed = null!;

    void Because()
    {
        var plan = Plan(With([Input("kit", "1", "one")], [Requirement("r1", hash: "aa")]));
        _original = plan.WithInputComponents([new(ArtifactRenderComponentKind.Kit, "acme", "1.0.0", null)]);
        _changed = plan.WithInputComponents([new(ArtifactRenderComponentKind.Kit, "acme", "1.0.1", null)]);
    }

    [Fact] void should_preserve_the_output_digest() => _changed.Digest.ShouldEqual(_original.Digest);
    [Fact] void should_change_the_provenance_digest() => _changed.InputProvenance.Digest.ShouldNotEqual(_original.InputProvenance.Digest);
}
