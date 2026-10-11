// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_planning_with_a_profile;

public class with_replaced_scaffold_bytes : given.a_profile_source_plan
{
    void Establish()
    {
        var program = _profile.Inputs.Single(input => input.Name == "cratis-scaffold:text:Program.cs");
        _options = _options with
        {
            Profile = ArtifactRenderProfile.Create(
                _profile.Target,
                _profile.TargetVersion,
                _profile.Renderer,
                _profile.RendererVersion,
                [.. _profile.Inputs.Where(input => input != program), ArtifactRenderInput.Create(program.Name, program.Version, program.Bytes.Add(0))])
        };
    }

    void Because() => _result = CratisRendering.PlanFrom(_loaded, _selection, _options);
    [Fact] void should_refuse_the_profile() => _result.Success.ShouldBeFalse();
    [Fact] void should_return_the_typed_profile_diagnostic() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-CRATIS-001");
    [Fact] void should_emit_no_artifacts() => _result.Artifacts.ShouldBeEmpty();
}
