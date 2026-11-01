// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_RenderOutcomeContract;

public class when_checking_degradation_vectors : given.outcome_vectors
{
    [Theory]
    [InlineData("STAGE-DEGRADE-001")]
    [InlineData("STAGE-DEGRADE-002")]
    [InlineData("STAGE-DEGRADE-003")]
    [InlineData("STAGE-DEGRADE-004")]
    void should_accept_each_root_warning_without_changing_its_severity_or_output(string code)
    {
        var plan = Plan([Degraded(1, code)]);
        var digest = plan.Digest;
        Assert.Empty(RenderOutcomeContract.Problems(plan, []));
        Assert.True(plan.Success);
        Assert.Equal(ArtifactRenderOutcome.Degraded, plan.Outcome);
        Assert.Equal(ArtifactRenderDiagnosticSeverity.Warning, Assert.Single(plan.Diagnostics).Severity);
        Assert.Contains("interface IContract", Encoding.UTF8.GetString(Assert.Single(plan.Artifacts).Bytes.AsSpan()));
        Assert.Equal(digest, plan.Digest);
    }

    [Fact]
    void should_accept_dependency_warning_005_with_root_and_two_hop_cascade()
    {
        var root = Degraded(1, "STAGE-DEGRADE-002");
        var middle = Degraded(2, "STAGE-DEGRADE-005", [new(root.Identity, "STAGE-DEGRADE-002")]);
        var leaf = Degraded(3, "STAGE-DEGRADE-005", [new(middle.Identity, "STAGE-DEGRADE-005")]);
        var plan = Plan([root, middle, leaf]);
        Assert.Empty(RenderOutcomeContract.Problems(plan, []));
        Assert.Equal(3, plan.Inventory!.Score.Counts.Degraded);
        Assert.Equal(new ArtifactRenderGapReference(root.Identity, "STAGE-DEGRADE-002"), Assert.Single(middle.Degradations[0].Roots));
        Assert.Equal(new ArtifactRenderGapReference(middle.Identity, "STAGE-DEGRADE-005"), Assert.Single(leaf.Degradations[0].Roots));
    }

    [Fact]
    void should_accept_strict_promotions_only_with_matching_original_evidence()
    {
        var normal = Plan([Degraded(1, "STAGE-DEGRADE-002")]);
        var strict = normal.WithAcceptance(normal.Degradations, strict: true);
        Assert.Empty(RenderOutcomeContract.Problems(strict, []));
        Assert.False(strict.Success);
        Assert.True(strict.CanPublishArtifacts);
        Assert.Equal(ArtifactRenderOutcome.Degraded, strict.Outcome);
        Assert.Equal(ArtifactRenderDiagnosticSeverity.Error, Assert.Single(strict.Diagnostics).Severity);
        Assert.Equal(normal.Artifacts, strict.Artifacts);
    }

    [Fact]
    void should_reject_a_missing_retained_non_leaf_placeholder()
    {
        var plan = Plan([Degraded(1, "STAGE-DEGRADE-002")], retain: false);
        Assert.Equal($"Missing retained placeholder: {Id(1)}.", Assert.Single(RenderOutcomeContract.Problems(plan, [])));
    }
}
