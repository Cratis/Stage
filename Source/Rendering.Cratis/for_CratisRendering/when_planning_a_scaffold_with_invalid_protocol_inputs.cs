// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_planning_a_scaffold_with_invalid_protocol_inputs : Specification
{
    CratisPlanOptions _options = null!;
    ArtifactRenderProfile _profile = null!;
    CratisPlanResult[] _results = [];

    void Establish()
    {
        _options = new("Shop", "Shop.Backend", "Acme.Shop");
        _profile = CratisRendering.CreateProfile(_options.ApplicationName, new(_options.ProjectName, _options.RootNamespace));
    }

    void Because()
    {
        ArtifactRenderInput[][] invalidInputs =
        [
            [ArtifactRenderInput.Create(StrictAcceptanceInput.Name, "999", [.. "true"u8])],
            [ArtifactRenderInput.Create(StrictAcceptanceInput.Name, StrictAcceptanceInput.Version, [.. "TRUE"u8])],
            [StrictAcceptanceInput.Create(true), ArtifactRenderInput.Create(StrictAcceptanceInput.Name, "999", [.. "false"u8])],
            [ArtifactRenderInput.Create("stage:unknown", "1", [.. "true"u8])],
            [ArtifactRenderInput.Create("cratis-render:unknown", "1", [.. "{}"u8])],
            [ArtifactRenderInput.Create("cratis-scaffold:text:unknown.txt", "1", [.. "unknown"u8])]
        ];
        _results = [.. invalidInputs.Select(inputs => CratisRendering.PlanScaffold(_options with
        {
            Profile = ArtifactRenderProfile.Create(_profile.Target, _profile.TargetVersion, _profile.Renderer, _profile.RendererVersion, [.. _profile.Inputs, .. inputs])
        }))];
    }

    [Fact] void should_refuse_every_invalid_profile() => _results.All(result => !result.Success).ShouldBeTrue();
    [Fact] void should_report_profile_admission_failures() => _results.All(result => result.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-CRATIS-001")).ShouldBeTrue();
    [Fact] void should_emit_no_artifacts() => _results.All(result => result.Artifacts.IsEmpty).ShouldBeTrue();
}
