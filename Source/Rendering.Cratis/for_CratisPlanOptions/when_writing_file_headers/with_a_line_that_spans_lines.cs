// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisPlanOptions.when_writing_file_headers;

public class with_a_line_that_spans_lines : a_register_project_render_request
{
    CratisPlanResult _result = null!;
    Exception? _error;

    void Because()
    {
        _result = CratisRendering.PlanScaffold(new(_model.Application.Name, "Projects", "Projects") { FileHeader = ["Copyright\nAcme"] });
        _error = Catch.Exception(() => CratisRendering.CreateProfile(_model.Application.Name, _options with { FileHeader = ["Acme\r"] }));
    }

    [Fact] void should_refuse_the_plan() => _result.Diagnostics.Single().Code.ShouldEqual("STAGE-PLAN-023");
    [Fact] void should_plan_no_artifacts() => _result.Artifacts.ShouldBeEmpty();
    [Fact] void should_refuse_the_profile() => _error.ShouldBeOfExactType<InvalidCratisBackendApplicationScaffold>();
}
