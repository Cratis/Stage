// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan;

public class when_normalizing_artifact_sources : given.an_artifact_render_request
{
    ArtifactRenderPlan _plan = null!;
    Exception _unset = null!;
    Exception _default = null!;
    string _originalHash = null!;
    SemanticId _first;
    SemanticId _last;

    void Because()
    {
        _first = SemanticId.Parse($"sem1:{new string('0', 64)}");
        _last = SemanticId.Parse($"sem1:{new string('f', 64)}");
        var artifact = PlannedArtifact.CreateText("source.cs", "unchanged", [_last, _first, _first]);
        _originalHash = artifact.Sha256;
        _plan = ArtifactRenderPlan.Create(_request, [artifact], []);
        _unset = Catch.Exception(() => PlannedArtifact.CreateText("unset.cs", "text", [default]));
        _default = Catch.Exception(() => PlannedArtifact.CreateText("default.cs", "text", default));
    }

    [Fact] void should_sort_and_deduplicate_the_sources() => _plan.Artifacts.Single().Sources.SequenceEqual([_first, _last]).ShouldBeTrue();
    [Fact] void should_leave_the_byte_hash_unchanged() => _plan.Artifacts.Single().Sha256.ShouldEqual(_originalHash);
    [Fact] void should_reject_unset_sources() => _unset.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_reject_default_source_arrays() => _default.ShouldBeOfExactType<InvalidArtifactRenderContract>();
}
