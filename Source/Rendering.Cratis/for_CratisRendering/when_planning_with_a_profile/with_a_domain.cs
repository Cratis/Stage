// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_planning_with_a_profile;

public class with_a_domain : given.a_profile_source_plan
{
    void Establish()
    {
        _options = _options with { Domain = "Sales" };
        PlanFull();
    }

    void Because() => _result = CratisRendering.PlanFrom(_loaded, _selection, _options);
    [Fact] void should_plan_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_a_successful_full_baseline() => _full.Success.ShouldBeTrue();
    [Fact] void should_render_scoped_artifacts() => _result.Artifacts.ShouldNotBeEmpty();
    [Fact] void should_match_every_full_render_artifact_byte() => ShouldMatchFull();
    [Fact] void should_place_the_command_in_the_domain() => _result.Artifacts.Select(artifact => artifact.RelativePath).ShouldContain("Sales/Projects/Registration/RegisterProject/RegisterProject.cs");
    [Fact] void should_preserve_authored_documentation() => CommandText().ShouldContain("/// Registers &lt;project&gt; &amp; name");
}
