// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan.when_recording_input_provenance;

public class and_a_requirement_is_null : given.a_provenance_request
{
    ArtifactRenderPlan _without = null!;
    ArtifactRenderPlan _with = null!;

    void Because()
    {
        _without = Plan(With([Input("kit", "1", "one")], [Requirement("r1")]));
        _with = Plan(With([Input("kit", "1", "one")], [Requirement("r1"), null!]));
    }

    [Fact] void should_omit_the_null_entry() => _with.InputProvenance.Implementations.Select(_ => (_.RequirementId, _.ContentHash)).ShouldEqual(_without.InputProvenance.Implementations.Select(_ => (_.RequirementId, _.ContentHash)));
    [Fact] void should_have_the_same_provenance_digest() => _with.InputProvenance.Digest.ShouldEqual(_without.InputProvenance.Digest);
}
