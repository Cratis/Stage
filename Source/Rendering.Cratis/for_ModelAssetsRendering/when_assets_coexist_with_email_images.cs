// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ModelAssetsRendering;

public class when_assets_coexist_with_email_images : for_RealizedApplication.given.a_model_with_authentication_and_onboarding
{
    void Establish()
    {
        Directory.CreateDirectory(Path.Combine(_folder, "assets"));
        File.WriteAllBytes(Path.Combine(_folder, "assets", "logo.png"), [3, 2, 1]);
    }

    void Because()
    {
        var profile = CratisRendering.WithModelAssets(
            CratisRendering.WithRealization(CratisRendering.CreateProfile(Application, new(Application, Application)), _loaded.Realization),
            _folder);
        _plan = new CratisArtifactRenderPlanner().Plan(new(_loaded.Model, _loaded.Plan, profile, new(ArtifactRenderScopeKind.Application, _loaded.Model.Application.Id)));
    }

    [Fact] void should_emit_the_asset() => _plan.Artifacts.Single(artifact => artifact.RelativePath == ".frontend/public/assets/logo.png").Bytes.ShouldEqual(new byte[] { 3, 2, 1 });
    [Fact] void should_keep_the_email_image() => _plan.Artifacts.Single(artifact => artifact.RelativePath == "Emails/images/logo.png").Bytes.ShouldEqual(Png);
}
