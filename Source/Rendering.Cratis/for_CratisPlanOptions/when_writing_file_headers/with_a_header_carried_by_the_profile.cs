// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisPlanOptions.when_writing_file_headers;

public class with_a_header_carried_by_the_profile : a_register_project_render_request
{
    CratisPlanResult _carried = null!;
    CratisPlanResult _twice = null!;

    void Because()
    {
        var profile = CratisRendering.CreateProfile(_model.Application.Name, _options with { FileHeader = ["Acme"] });
        var options = new CratisPlanOptions(_model.Application.Name, "Projects", "Projects") { Profile = profile };
        _carried = CratisRendering.PlanScaffold(options);
        _twice = CratisRendering.PlanScaffold(options with { FileHeader = ["Acme"] });
    }

    [Fact] void should_plan_the_scaffold() => _carried.Success.ShouldBeTrue();
    [Fact] void should_write_the_carried_header() => Text(_carried.Artifacts.Single(artifact => artifact.RelativePath == "Program.cs")).StartsWith("// Acme\n\nusing ", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_refuse_a_second_header_from_the_options() => _twice.Diagnostics.Single().Code.ShouldEqual("STAGE-CRATIS-001");
}
