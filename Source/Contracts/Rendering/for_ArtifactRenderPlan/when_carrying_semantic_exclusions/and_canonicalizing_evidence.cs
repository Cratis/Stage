// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan.when_carrying_semantic_exclusions;

public class and_canonicalizing_evidence : given.a_request_with_exclusions
{
    ArtifactRenderPlan _reordered = null!;

    void Because()
    {
        _result = Create();
        _reordered = ArtifactRenderPlan.Create(_request with { Exclusions = [_specification, _slice with { Diagnostics = [.. _slice.Diagnostics.Reverse()] }] }, [], _result.Diagnostics);
    }

    [Fact] void should_canonicalize_the_digest() => _reordered.Digest.ShouldEqual(_result.Digest);
    [Fact] void should_not_duplicate_warnings_when_reconstructing() => _reordered.Diagnostics.Length.ShouldEqual(2);
    [Fact] void should_change_the_digest_when_metadata_changes() => ArtifactRenderPlan.Create(_request with { Exclusions = [_slice with { Reason = "Changed reason" }, _specification] }, [], []).Digest.ShouldNotEqual(_result.Digest);
    [Fact] void should_leave_legacy_digests_unchanged() => ArtifactRenderPlan.Create(_request with { Exclusions = [] }, [], []).Digest.ShouldEqual(ArtifactRenderPlan.Create(new(_request.Model, _request.ExecutionPlan, _request.Profile, _request.Scope), [], []).Digest);
}
