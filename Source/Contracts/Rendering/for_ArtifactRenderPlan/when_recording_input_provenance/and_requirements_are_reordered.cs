// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan.when_recording_input_provenance;

public class and_requirements_are_reordered : given.a_provenance_request
{
    ArtifactRenderPlan _first = null!;
    ArtifactRenderPlan _second = null!;

    void Because()
    {
        _first = Plan(With([], [Requirement("a"), Requirement("b"), Requirement("c")]));
        _second = Plan(With([], [Requirement("c"), Requirement("a"), Requirement("b")]));
    }

    [Fact] void should_record_the_same_provenance() => _second.InputProvenance.Implementations.SequenceEqual(_first.InputProvenance.Implementations).ShouldBeTrue();
    [Fact] void should_have_the_same_provenance_digest() => _second.InputProvenance.Digest.ShouldEqual(_first.InputProvenance.Digest);
}
