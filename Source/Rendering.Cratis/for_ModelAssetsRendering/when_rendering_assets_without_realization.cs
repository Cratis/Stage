// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ModelAssetsRendering;

public class when_rendering_assets_without_realization : for_CratisRendering.given.a_source_plan
{
    static readonly byte[] _binary = [0, 255, 128, 10, 13];
    CratisPlanResult _withoutAssets = null!;
    ArtifactRenderPlan _application = null!;
    CratisPlanResult _featurePlan = null!;
    ArtifactRenderPlan _changed = null!;
    ArtifactRenderPlan _removed = null!;
    ArtifactRenderPlan _repository = null!;
    ArtifactRenderProfile _firstProfile = null!;
    ArtifactRenderProfile _changedProfile = null!;
    LoadedSemanticModel _withAssets = null!;

    void Establish()
    {
        Directory.CreateDirectory(Path.Combine(_root, "assets", "nested"));
        File.WriteAllBytes(Path.Combine(_root, "assets", "nested", "picture.png"), _binary);
        File.WriteAllText(Path.Combine(_root, "assets", "logo.svg"), "<svg/>");
        File.WriteAllText(Path.Combine(_root, "assets", ".hidden.svg"), "hidden");
        File.WriteAllText(Path.Combine(_root, "secret.json"), "outside assets");
    }

    async Task Because()
    {
        _withoutAssets = CratisRendering.PlanFrom(_loaded, _featureSelection, _planOptions);
        _withAssets = (await SemanticModelLoader.LoadAsync(_root, [], null, _planOptions.ApplicationName)).Loaded!;
        _featurePlan = await From();
        _firstProfile = Profile(_options);
        _application = Application(_withAssets, _firstProfile);
        _repository = Application(_withAssets, Profile(_options with { ServiceLayout = true }));
        File.WriteAllBytes(Path.Combine(_root, "assets", "nested", "picture.png"), [3, 2, 1]);
        _changedProfile = Profile(_options);
        _changed = Application(_withAssets, _changedProfile);
        File.Delete(Path.Combine(_root, "assets", "nested", "picture.png"));
        _removed = Application(_withAssets, Profile(_options));
    }

    ArtifactRenderProfile Profile(CratisRenderingOptions options) => CratisRendering.WithModelAssets(CratisRendering.CreateProfile(_loaded.Model.Application.Name, options), _root);
    static ArtifactRenderPlan Application(LoadedSemanticModel loaded, ArtifactRenderProfile profile) => new CratisArtifactRenderPlanner().Plan(new(loaded.Model, loaded.Plan, profile, new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id)));

    [Fact] void should_render_without_authentication_or_onboarding() => _application.Diagnostics.Where(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_preserve_binary_bytes() => Asset(_application, ".frontend/public/assets/nested/picture.png").Bytes.ShouldEqual(_binary);
    [Fact] void should_render_binary_kind() => Asset(_application, ".frontend/public/assets/logo.svg").Kind.ShouldEqual(PlannedArtifactKind.Binary);
    [Fact] void should_render_svg() => Asset(_application, ".frontend/public/assets/logo.svg").Bytes.ShouldEqual(System.Text.Encoding.UTF8.GetBytes("<svg/>"));
    [Fact] void should_not_render_hidden_or_outside_files() => _application.Artifacts.Where(artifact => artifact.RelativePath.Contains("hidden", StringComparison.Ordinal) || artifact.RelativePath.Contains("secret.json", StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_keep_scoped_plans_scaffold_free() => _featurePlan.Artifacts.Any(artifact => artifact.RelativePath.StartsWith(".frontend/public/assets/", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_keep_the_semantic_revision() => _changed.SemanticRevision.ShouldEqual(_withoutAssets.Revision!.Value);
    [Fact] void should_attach_assets_at_load_time() => _withAssets.ModelAssets.Files["nested/picture.png"].ShouldEqual(_binary);
    [Fact] void should_carry_assets_through_source_planning() => _featurePlan.Plan!.InputProvenance.Inputs.ShouldContain(input => input.Name == Realization.ModelAssetsInput.Name);
    [Fact] void should_carry_assets_without_io_when_loaded() => CratisRendering.PlanFrom(_withAssets, _featureSelection, _planOptions).Plan!.InputProvenance.Inputs.SequenceEqual(_featurePlan.Plan!.InputProvenance.Inputs).ShouldBeTrue();
    [Fact] void should_track_assets_on_the_application() => Asset(_application, ".frontend/public/assets/nested/picture.png").Sources.ShouldContain(_loaded.Model.Application.Id);
    [Fact] void should_allow_binary_assets_in_publication() => Assert.IsType<CratisPublicationCheck.Compatible>(CratisRendering.CheckPublication(_application, path => _application.Artifacts.FirstOrDefault(artifact => artifact.RelativePath == path && artifact.Kind == PlannedArtifactKind.Text) is { } text ? System.Text.Encoding.UTF8.GetString(text.Bytes.AsSpan()) : null));
    [Fact] void should_change_the_output_digest() => _changed.Digest.ShouldNotEqual(_application.Digest);
    [Fact] void should_remove_deleted_assets_from_the_plan() => _removed.Artifacts.Any(artifact => artifact.RelativePath == ".frontend/public/assets/nested/picture.png").ShouldBeFalse();
    [Fact] void should_change_the_hashed_asset_input() => _changedProfile.Inputs.Single(input => input.Name == Realization.ModelAssetsInput.Name).Sha256.ShouldNotEqual(_firstProfile.Inputs.Single(input => input.Name == Realization.ModelAssetsInput.Name).Sha256);
    [Fact] void should_prefix_service_assets() => Asset(_repository, $"Source/{_options.ProjectName}/.frontend/public/assets/nested/picture.png").Bytes.ShouldEqual(_binary);
    [Fact] void should_not_interpret_metadata_as_a_scaffold_file() => _application.Artifacts.Any(artifact => artifact.RelativePath.Contains("cratis-realization:model-assets", StringComparison.Ordinal)).ShouldBeFalse();

    static PlannedArtifact Asset(ArtifactRenderPlan result, string path) => result.Artifacts.Single(artifact => artifact.RelativePath == path);
}
