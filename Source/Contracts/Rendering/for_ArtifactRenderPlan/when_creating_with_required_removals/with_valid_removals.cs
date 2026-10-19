// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan.when_creating_with_required_removals;

public class with_valid_removals : given_removals
{
    ArtifactRenderPlan _plan = null!;
    ArtifactRenderPlan _withoutRemovals = null!;

    void Because()
    {
        _plan = Create(new ArtifactRemoval("Orders\\Totals\\Zeta.cs", Hash, _model), new ArtifactRemoval("Orders/Totals/Fold.cs", Hash, _model));
        _withoutRemovals = ArtifactRenderPlan.Create(_request, _artifacts, [], [], []);
    }

    [Fact] void should_normalize_and_order_removals() => _plan.RequiredRemovals.Select(_ => _.RelativePath).SequenceEqual(["Orders/Totals/Fold.cs", "Orders/Totals/Zeta.cs"]).ShouldBeTrue();
    [Fact] void should_keep_the_digest_when_empty() => Create().Digest.ShouldEqual(_withoutRemovals.Digest);
    [Fact] void should_keep_the_original_digest_formula_when_empty() => _withoutRemovals.Digest.ShouldEqual(ArtifactRenderPlan.ComputeDigest(_withoutRemovals.Target, _withoutRemovals.TargetVersion, _withoutRemovals.Renderer, _withoutRemovals.RendererVersion, _withoutRemovals.ApplicationName, _withoutRemovals.Artifacts, _withoutRemovals.Diagnostics));
    [Fact] void should_change_the_digest_when_present() => _plan.Digest.ShouldNotEqual(_withoutRemovals.Digest);
    [Fact] void should_have_no_removals_by_default() => _withoutRemovals.RequiredRemovals.IsEmpty.ShouldBeTrue();
}
