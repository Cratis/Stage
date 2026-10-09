// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_scoped_shared_declarations;

public class with_a_composite_type_referencing_concepts : given.a_multi_module_application
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = Plan(new(ArtifactRenderScopeKind.Slice, _placeOrder.Id));

    [Fact] void should_include_the_direct_composite_without_churn() => SameArtifact(_application, _plan, "Common/ShippingAddress.cs").ShouldBeTrue();
    [Fact] void should_include_the_transitive_composite_without_churn() => SameArtifact(_application, _plan, "Common/Address.cs").ShouldBeTrue();
    [Fact] void should_include_the_transitive_concept_without_churn() => SameArtifact(_application, _plan, "Common/PostalCode.cs").ShouldBeTrue();
}
