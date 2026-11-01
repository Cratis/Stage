// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures;

public class when_admitting_v34 : a_register_project_render_request
{
    ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        typeof(ExecutableSemanticModel).GetField("<LanguageVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_model, LanguageVersion.V34);
        typeof(ExecutableSemanticModel).GetField("<SemanticVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_model, SemanticVersion.V34);
    }

    void Because() => _plan = _planner.Plan(_request);

    [Fact]
    void should_render_without_invocation_expectations()
    {
        SemanticVersionFeatures.Supports(_model).ShouldBeTrue();
        _plan.Success.ShouldBeTrue();
        _plan.CanPublishArtifacts.ShouldBeTrue();
        _plan.Artifacts.ShouldNotBeEmpty();
    }
}
