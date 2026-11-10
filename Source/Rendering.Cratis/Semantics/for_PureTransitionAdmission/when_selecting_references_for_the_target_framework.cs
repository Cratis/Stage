// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_PureTransitionAdmission;

public class when_selecting_references_for_the_target_framework : a_register_project_render_request
{
    MetadataReference[] _version3 = null!;
    MetadataReference[] _version4 = null!;

    void Because()
    {
        _version3 = PureTransitionAdmission.AnalysisReferences(ContextFor(CratisBackendApplicationScaffoldProfile.Version3).TargetFramework);
        _version4 = PureTransitionAdmission.AnalysisReferences(ContextFor(CratisBackendApplicationScaffoldProfile.ForVersion("4")!).TargetFramework);
    }

    [Fact] void should_use_the_unchanged_net10_pack_for_version_3() => Assert.Same(PureTransitionAdmission._references.Value, _version3);
    [Fact] void should_use_the_net11_pack_for_version_4() => Assert.Same(PureTransitionAdmission._references11.Value, _version4);
    [Fact] void should_embed_the_net11_pack_version_in_the_version_4_references() =>
        Assert.Contains(_version4, reference => reference.Display!.StartsWith($"PureTransitionReferences.{PureTransitionAdmission.ReferencePackVersion11}.", StringComparison.Ordinal));
    [Fact] void should_key_the_pack_version_by_framework() =>
        PureTransitionAdmission.ReferencePackVersionFor("net11.0").ShouldNotEqual(PureTransitionAdmission.ReferencePackVersionFor("net10.0"));

    SemanticApplicationContext ContextFor(CratisBackendApplicationScaffoldProfile scaffold) =>
        new(new(_model, _executionPlan, CratisRendering.CreateProfile(_model.Application.Name, _options, null, null, null, null, false, scaffold), new(ArtifactRenderScopeKind.Application, _model.Application.Id)), _options);
}
