// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan;

public class when_retaining_inline_input_provenance : given.an_artifact_render_request
{
    ArtifactRenderPlan _original = null!;
    void Establish() => _original = ArtifactRenderPlan.Create(_request, [PlannedArtifact.CreateText("same.cs", "same")], []);

    [Fact]
    void should_leave_the_digest_unchanged_without_inline_inputs()
    {
        Assert.Null(_original.InlineCodeFingerprint);
        _original.WithInlineCodeFingerprint(null).Digest.ShouldEqual(_original.Digest);
    }

    [Fact]
    void should_hash_inputs_independently_of_output_and_retain_them_in_copies()
    {
        var fingerprint = new string('a', 64);
        var plan = _original.WithInlineCodeFingerprint(fingerprint);
        plan.Digest.ShouldNotEqual(_original.Digest);
        plan.WithInlineCodeFingerprint(new string('b', 64)).Digest.ShouldNotEqual(plan.Digest);
        plan.Artifacts.Single().Sha256.ShouldEqual(_original.Artifacts.Single().Sha256);
        plan.WithInventory(ArtifactRenderInventory.Create([])).WithPlanEvidence([]).WithAcceptance([], strict: true)
            .InlineCodeFingerprint.ShouldEqual(fingerprint);
        Assert.Null(_original.InlineCodeFingerprint);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    void should_reject_invalid_fingerprints(string fingerprint) =>
        Assert.Throws<InvalidArtifactRenderContract>(() => _original.WithInlineCodeFingerprint(fingerprint));
}
