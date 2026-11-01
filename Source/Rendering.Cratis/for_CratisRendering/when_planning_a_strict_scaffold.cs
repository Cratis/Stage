// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_planning_a_strict_scaffold : Specification
{
    CratisPlanOptions _options = null!;
    CratisPlanResult _default = null!;
    CratisPlanResult _strict = null!;
    CratisPlanResult _carried = null!;
    ArtifactRenderProfile _profile = null!;

    void Establish()
    {
        _options = new("Shop", "Shop.Backend", "Acme.Shop");
        _default = CratisRendering.PlanScaffold(_options);
        _profile = CratisRendering.CreateProfile(_options.ApplicationName, new(_options.ProjectName, _options.RootNamespace) { Strict = true });
    }

    void Because()
    {
        _strict = CratisRendering.PlanScaffold(_options with { Strict = true });
        _carried = CratisRendering.PlanScaffold(_options with { Profile = _profile });
    }

    [Fact] void should_accept_the_strict_option() => _strict.Success.ShouldBeTrue();
    [Fact] void should_accept_an_admitted_strict_profile() => _carried.Success.ShouldBeTrue();
    [Fact] void should_preserve_the_default_safe_artifact_paths() => _strict.Artifacts.Select(artifact => artifact.RelativePath).ShouldEqual(_default.Artifacts.Select(artifact => artifact.RelativePath));
    [Fact] void should_preserve_the_default_output_bytes_and_diagnostics() => _strict.Digest.ShouldEqual(_default.Digest);
    [Fact] void should_render_the_same_output_from_a_carried_strict_profile() => _carried.Digest.ShouldEqual(_default.Digest);
    [Fact] void should_not_emit_strict_metadata() => _strict.Artifacts.Select(artifact => artifact.RelativePath).ShouldNotContain(StrictAcceptanceInput.Name);
    [Fact] void should_retain_strict_metadata_in_the_profile() => _profile.Inputs.Count(input => input.Name == StrictAcceptanceInput.Name).ShouldEqual(1);
}
