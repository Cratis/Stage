// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_semantic_exclusions;

public class and_receiving_a_scoped_request : given.an_excluded_feature
{
    void Establish() => _request = _request with { Scope = new(ArtifactRenderScopeKind.Slice, _registerProject.Id) };
    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_not_filter_out_caller_supplied_exclusions() => _plan.Exclusions.Single().Address.ShouldEqual(_exclusion.Address);
    [Fact] void should_comment_on_the_retained_parent_not_a_fabricated_leaf() => Comment.Sources.Single().ShouldEqual(_feature.Id);
    [Fact] void should_keep_the_scoped_generated_command() => _plan.Artifacts.Any(artifact => artifact.RelativePath.EndsWith("/RegisterProject.cs", StringComparison.Ordinal)).ShouldBeTrue();
}
