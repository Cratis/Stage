// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisFrontendApplicationScaffold.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_planning_with_a_profile;

public class with_approved_packages : given.a_profile_source_plan
{
    CratisPlanResult _scaffold = null!;
    CratisPlanResult _tampered = null!;
    CratisPlanResult _reserved = null!;

    void Establish() =>
        _options = _options with { Profile = Profile(an_approved_package_set.Document) };

    void Because()
    {
        _scaffold = CratisRendering.PlanScaffold(_options);
        _tampered = CratisRendering.PlanScaffold(_options with { Profile = Replace(_options.Profile, "approved-packages.json", an_approved_package_set.Document.Replace("1.2.0", "9.9.9", StringComparison.Ordinal)) });
        _reserved = CratisRendering.PlanScaffold(_options with { Profile = Replace(_options.Profile, "approved-packages.json", an_approved_package_set.Document.Replace("Acme.Charts", "core", StringComparison.Ordinal)) });
    }

    [Fact] void should_plan_the_scaffold_successfully() => _scaffold.Success.ShouldBeTrue();
    [Fact] void should_emit_the_approved_package_set_document() => _scaffold.Artifacts.Select(artifact => artifact.RelativePath).ShouldContain("approved-packages.json");
    [Fact] void should_emit_the_module_that_loads_it() => _scaffold.Artifacts.Select(artifact => artifact.RelativePath).ShouldContain(".frontend/approvedPackages.ts");
    [Fact] void should_refuse_a_document_that_the_scaffold_was_not_generated_from() => _tampered.Success.ShouldBeFalse();
    [Fact] void should_report_the_typed_profile_diagnostic_for_it() => _tampered.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-CRATIS-001");
    [Fact] void should_refuse_a_package_reusing_a_shipped_name() => _reserved.Success.ShouldBeFalse();

    ArtifactRenderProfile Profile(string document) =>
        CratisRendering.CreateProfile("Projects", new("Projects", "Projects"), null, null, null, global::Cratis.Scene.Engine.Packages.ApprovedPackageSetJson.Read(document, CratisRendering.ShippedPackageNames).Set);

    static ArtifactRenderProfile Replace(ArtifactRenderProfile profile, string path, string content)
    {
        var name = $"cratis-scaffold:text:{path}";
        var existing = profile.Inputs.Single(input => input.Name == name);
        return ArtifactRenderProfile.Create(
            profile.Target,
            profile.TargetVersion,
            profile.Renderer,
            profile.RendererVersion,
            [.. profile.Inputs.Where(input => input != existing), ArtifactRenderInput.Create(name, existing.Version, [.. Encoding.UTF8.GetBytes(content)])]);
    }
}
