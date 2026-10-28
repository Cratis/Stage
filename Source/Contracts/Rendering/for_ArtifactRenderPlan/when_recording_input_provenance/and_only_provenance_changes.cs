// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan.when_recording_input_provenance;

public class and_only_provenance_changes : given.a_provenance_request
{
    ArtifactRenderPlan _original = null!;
    ArtifactRenderPlan _changed = null!;

    void Because()
    {
        _original = Plan(With([Input("kit", "1", "one")], [Requirement("r1", hash: "aa")]));
        _changed = Plan(With([Input("kit", "1", "two")], [Requirement("r1", hash: "bb")]));
    }

    [Fact] void should_preserve_the_output_digest() => _changed.Digest.ShouldEqual(_original.Digest);
    [Fact] void should_change_the_provenance_digest() => _changed.InputProvenance.Digest.ShouldNotEqual(_original.InputProvenance.Digest);
}
