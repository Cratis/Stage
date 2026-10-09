// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_scoped_shared_declarations;

public class with_unreferenced_declarations : given.a_multi_module_application
{
    ArtifactRenderPlan _slice = null!;
    ArtifactRenderPlan _module = null!;
    ArtifactRenderPlan _feature = null!;

    void Because()
    {
        _slice = Plan(new(ArtifactRenderScopeKind.Slice, _placeOrder.Id));
        _module = Plan(new(ArtifactRenderScopeKind.Module, _customers.Id));
        _feature = Plan(new(ArtifactRenderScopeKind.Feature, _sales.Features.Single().Id));
    }

    [Fact] void should_exclude_unused_concepts_from_every_scoped_plan() => Plans().All(plan => plan.Artifacts.All(artifact => artifact.RelativePath != "Common/Unused.cs")).ShouldBeTrue();
    [Fact] void should_exclude_unused_composites_from_every_scoped_plan() => Plans().All(plan => plan.Artifacts.All(artifact => artifact.RelativePath != "Common/UnusedType.cs")).ShouldBeTrue();
    [Fact] void should_not_include_sales_concepts_in_the_customer_module() => _module.Artifacts.Select(artifact => artifact.RelativePath).ShouldNotContain("Common/Money.cs");
    [Fact] void should_keep_all_declarations_in_the_application_plan() => _application.Artifacts.Select(artifact => artifact.RelativePath).ShouldContain("Common/UnusedType.cs");
    [Fact] void should_keep_shared_bytes_in_a_multiple_slice_feature_plan() => _feature.Artifacts.Where(artifact => artifact.RelativePath.StartsWith("Common/", StringComparison.Ordinal)).All(artifact => SameArtifact(_application, _feature, artifact.RelativePath)).ShouldBeTrue();
    [Fact] void should_not_include_the_scaffold_in_scoped_plans() => Plans().All(plan => plan.Artifacts.All(artifact => artifact.RelativePath != "Shop.csproj")).ShouldBeTrue();

    IReadOnlyList<ArtifactRenderPlan> Plans() => [_slice, _module, _feature];
}
