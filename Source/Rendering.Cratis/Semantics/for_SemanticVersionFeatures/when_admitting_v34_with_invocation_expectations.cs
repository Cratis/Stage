// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures;

public class when_admitting_v34_with_invocation_expectations : a_register_project_render_request
{
    ArtifactRenderPlan _plan = null!;
    SemanticSpecification _specification = null!;

    void Establish()
    {
        typeof(ExecutableSemanticModel).GetField("<LanguageVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_model, LanguageVersion.V34);
        typeof(ExecutableSemanticModel).GetField("<SemanticVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_model, SemanticVersion.V34);
        _specification = _registerProject.Specifications[0];
        var invocations = ImmutableArray.Create(new SemanticSpecificationInvocation(_registerProject.Commands.Single().Id, []));
        typeof(SemanticSpecification).GetProperty(nameof(SemanticSpecification.ThenInvocations)).SetValue(_specification, invocations);
    }

    void Because() => _plan = _planner.Plan(_request);

    [Fact]
    void should_refuse_invocation_expectations_per_construct()
    {
        SemanticVersionFeatures.Supports(_model).ShouldBeTrue();
        _plan.Success.ShouldBeFalse();
        _plan.CanPublishArtifacts.ShouldBeFalse();
        _plan.Artifacts.ShouldBeEmpty();
        _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-016" && diagnostic.Artifact == _specification.Id && diagnostic.Message.Contains("ThenInvocations", StringComparison.Ordinal));
        _plan.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == "CLI-RENDER-004");
    }
}
