// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisPlanOptions;

public class when_selecting_signed_forwarded_identity : a_register_project_render_request
{
    CratisPlanOptions _plan = null!;
    CratisPlanResult _default = null!;
    CratisPlanResult _explicitlyOff = null!;
    CratisPlanResult _signed = null!;
    CratisPlanResult _scaffold = null!;
    CratisPlanResult _signedScaffold = null!;
    CratisPlanResult _alreadyCarried = null!;
    CratisPlanResult _invalid = null!;
    ArtifactRenderPlan _defaultApplication = null!;
    ArtifactRenderPlan _signedApplication = null!;

    void Establish() => _plan = new(_model.Application.Name, "Projects", "Projects");

    void Because()
    {
        var loaded = new LoadedSemanticModel(_model, _executionPlan);
        var selection = new PlanSelection([PlanSelectionEntry.Slice(_module.Name, _feature.Name, _registerProject.Name)]);
        var profile = CratisRendering.CreateProfile(_model.Application.Name, new(_plan.ProjectName, _plan.RootNamespace));
        var carried = ArtifactRenderProfile.Create(profile.Target, profile.TargetVersion, profile.Renderer, profile.RendererVersion, [.. profile.Inputs, SignedForwardedIdentityInput.Create()]);
        var invalid = ArtifactRenderProfile.Create(profile.Target, profile.TargetVersion, profile.Renderer, profile.RendererVersion, [.. profile.Inputs, ArtifactRenderInput.Create(SignedForwardedIdentityInput.Name, "2", [.. "enabled"u8])]);
        var scope = new ArtifactRenderScope(ArtifactRenderScopeKind.Application, _model.Application.Id);
        _default = CratisRendering.PlanFrom(loaded, selection, _plan);
        _explicitlyOff = CratisRendering.PlanFrom(loaded, selection, _plan with { SignedForwardedIdentity = false });
        _signed = CratisRendering.PlanFrom(loaded, selection, _plan with { SignedForwardedIdentity = true });
        _scaffold = CratisRendering.PlanScaffold(_plan);
        _signedScaffold = CratisRendering.PlanScaffold(_plan with { SignedForwardedIdentity = true });
        _alreadyCarried = CratisRendering.PlanScaffold(_plan with { SignedForwardedIdentity = true, Profile = carried });
        _invalid = CratisRendering.PlanScaffold(_plan with { Profile = invalid });
        _defaultApplication = CratisRendering.Plan(_model, _executionPlan, scope, _options);
        _signedApplication = CratisRendering.Plan(_model, _executionPlan, scope, _options with { SignedForwardedIdentity = true });
    }

    [Fact] void should_default_to_unsigned() => _plan.SignedForwardedIdentity.ShouldBeFalse();
    [Fact] void should_not_put_signing_in_the_default_profile() => CratisRendering.CreateProfile(_model.Application.Name, _options).Inputs.Any(input => input.Name == SignedForwardedIdentityInput.Name).ShouldBeFalse();
    [Fact] void should_put_signing_in_the_profile_when_selected() => CratisRendering.CreateProfile(_model.Application.Name, _options with { SignedForwardedIdentity = true }).Inputs.Any(input => input.Name == SignedForwardedIdentityInput.Name).ShouldBeTrue();
    [Fact] void should_keep_the_default_plan_byte_identical() => _default.Digest.ShouldEqual(_explicitlyOff.Digest);

    // An application without authentication trusts no forwarded identity, so there is nothing to sign and nothing to warn about.
    [Fact] void should_plan_an_application_without_authentication() => _signed.Success.ShouldBeTrue();
    [Fact] void should_not_change_a_selection_without_authentication() => Describe(_signed.Artifacts).ShouldEqual(Describe(_default.Artifacts));
    [Fact] void should_not_warn_a_selection_without_authentication() => _default.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-AUTH-003");
    [Fact] void should_plan_an_application_without_authentication_when_selected() => _signedApplication.Diagnostics.Where(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_not_change_an_application_without_authentication() => Describe(_signedApplication.Artifacts).ShouldEqual(Describe(_defaultApplication.Artifacts));
    [Fact] void should_not_warn_an_application_without_authentication() => _signedApplication.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-AUTH-003");
    [Fact] void should_admit_the_option_for_a_scaffold() => _signedScaffold.Success.ShouldBeTrue();
    [Fact] void should_not_change_the_scaffold() => Describe(_signedScaffold.Artifacts).ShouldEqual(Describe(_scaffold.Artifacts));

    [Fact] void should_refuse_a_profile_that_already_carries_signing() => _alreadyCarried.Diagnostics.Single().Message.ShouldContain(nameof(CratisPlanOptions.SignedForwardedIdentity));
    [Fact] void should_refuse_a_profile_with_an_unknown_signing_version() => _invalid.Diagnostics.Single().Message.ShouldContain("signed forwarded identity");

    static string Describe(IEnumerable<PlannedArtifact> artifacts) =>
        string.Join('\n', artifacts.Select(artifact => $"{artifact.RelativePath}:{Convert.ToHexString(artifact.Bytes.AsSpan())}"));
}
