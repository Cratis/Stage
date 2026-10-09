// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_scoped_shared_declarations;

public class with_a_projection_over_another_modules_event : given.a_multi_module_application
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = Plan(new(ArtifactRenderScopeKind.Slice, _customerLookup.Id));

    [Fact] void should_plan_the_view() => _plan.Success.ShouldBeTrue();
    [Fact] void should_include_concepts_only_referenced_by_the_foreign_event() => SameArtifact(_application, _plan, "Common/CustomerRegion.cs").ShouldBeTrue();
    [Fact] void should_not_include_unreferenced_sales_concepts() => _plan.Artifacts.Select(artifact => artifact.RelativePath).ShouldNotContain("Common/Money.cs");
    [Fact] void should_not_expand_the_selection_to_the_foreign_slice() => _plan.Artifacts.Select(artifact => artifact.RelativePath).ShouldNotContain("Customers/Registration/Register/CustomerRegistered.cs");
}
