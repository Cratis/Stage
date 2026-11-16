// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ModelAssetsRendering;

public class when_assets_are_absent : for_CratisRendering.given.a_source_plan
{
    ArtifactRenderProfile _profile = null!;
    ArtifactRenderProfile _attached = null!;

    void Because()
    {
        _profile = CratisRendering.CreateProfile(_loaded.Model.Application.Name, _options);
        _attached = CratisRendering.WithModelAssets(_profile, _root);
    }

    [Fact] void should_leave_the_profile_unchanged() => Assert.Same(_profile, _attached);
    [Fact] void should_leave_the_application_digest_unchanged() => Plan(_attached).Digest.ShouldEqual(Plan(_profile).Digest);

    ArtifactRenderPlan Plan(ArtifactRenderProfile profile) => new CratisArtifactRenderPlanner().Plan(new(_loaded.Model, _loaded.Plan, profile, new(ArtifactRenderScopeKind.Application, _loaded.Model.Application.Id)));
}
