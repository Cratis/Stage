// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticApplicationContext;

public class when_selecting_the_target_framework : a_register_project_render_request
{
    string _default = null!;
    string _version3 = null!;
    string _version4 = null!;

    void Because()
    {
        _default = TargetFrameworkOf(CratisRendering.CreateProfile(_model.Application.Name, _options));
        _version3 = TargetFrameworkOf(CratisRendering.CreateProfile(_model.Application.Name, _options, null, null, null, null, false, CratisBackendApplicationScaffoldProfile.Version3));
        _version4 = TargetFrameworkOf(CratisRendering.CreateProfile(_model.Application.Name, _options, null, null, null, null, false, CratisBackendApplicationScaffoldProfile.ForVersion("4")!));
    }

    [Fact] void should_use_net10_for_the_default_profile() => _default.ShouldEqual("net10.0");
    [Fact] void should_use_net10_for_version_3() => _version3.ShouldEqual("net10.0");
    [Fact] void should_use_net11_for_version_4() => _version4.ShouldEqual("net11.0");

    string TargetFrameworkOf(ArtifactRenderProfile profile) =>
        new SemanticApplicationContext(new(_model, _executionPlan, profile, new(ArtifactRenderScopeKind.Application, _model.Application.Id)), _options).TargetFramework;
}
