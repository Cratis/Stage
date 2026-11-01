// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan.when_recording_input_provenance;

public class and_the_profile_has_inputs : given.a_provenance_request
{
    ArtifactRenderInput _kit = null!;
    ArtifactRenderInput _overlay = null!;
    ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        _kit = Input("kit", "1.0.0", "kit bytes");
        _overlay = Input("overlay:acme", "2.0.0", "overlay bytes");
    }

    void Because() => _plan = Plan(With([_overlay, _kit], []));

    [Fact] void should_record_name_version_and_digest_in_profile_order() =>
        _plan.InputProvenance.Inputs.SequenceEqual([new("kit", "1.0.0", _kit.Sha256), new("overlay:acme", "2.0.0", _overlay.Sha256)]).ShouldBeTrue();
    [Fact] void should_record_no_components() => _plan.InputProvenance.Components.ShouldBeEmpty();
}
