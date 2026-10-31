// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures;

public class when_admitting_v34 : a_register_project_render_request
{
    void Establish()
    {
        typeof(ExecutableSemanticModel).GetField("<LanguageVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_model, LanguageVersion.V34);
        typeof(ExecutableSemanticModel).GetField("<SemanticVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_model, SemanticVersion.V34);
    }

    [Fact]
    void should_render_without_invocation_expectations()
    {
        SemanticVersionFeatures.Supports(_model).ShouldBeTrue();
        var plan = _planner.Plan(_request);
        plan.Success.ShouldBeTrue();
        plan.CanPublishArtifacts.ShouldBeTrue();
        plan.Artifacts.ShouldNotBeEmpty();
    }

    [Fact]
    void should_refuse_invocation_expectations_per_construct()
    {
        var specification = _registerProject.Specifications[0];
        var invocations = ImmutableArray.Create(new SemanticSpecificationInvocation(_registerProject.Commands.Single().Id, []));
        typeof(SemanticSpecification).GetProperty(nameof(SemanticSpecification.ThenInvocations)).SetValue(specification, invocations);
        SemanticVersionFeatures.Supports(_model).ShouldBeTrue();
        var plan = _planner.Plan(_request);
        plan.Success.ShouldBeFalse();
        plan.CanPublishArtifacts.ShouldBeFalse();
        plan.Artifacts.ShouldBeEmpty();
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-016" && diagnostic.Artifact == specification.Id && diagnostic.Message.Contains("ThenInvocations", StringComparison.Ordinal));
        plan.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == "CLI-RENDER-004");
    }
}
