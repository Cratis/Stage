// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_scoped_shared_declarations;

public class with_a_concept_declared_in_another_module : given.a_multi_module_application
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = Plan(new(ArtifactRenderScopeKind.Slice, _placeOrder.Id));

    [Fact] void should_include_the_other_modules_document_concept_without_churn() => SameArtifact(_application, _plan, "Common/CustomerId.cs").ShouldBeTrue();
    [Fact] void should_keep_the_application_wide_identifier_classification() => Text(_plan.Artifacts.Single(artifact => artifact.RelativePath == "Common/CustomerId.cs")).ShouldContain("global::Cratis.Chronicle.Events.EventSourceId<global::System.Guid>");
}
