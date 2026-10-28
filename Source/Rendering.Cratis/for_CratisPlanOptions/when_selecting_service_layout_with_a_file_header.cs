// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisPlanOptions;

public class when_selecting_service_layout_with_a_file_header : a_register_project_render_request
{
    ArtifactRenderPlan _application = null!;
    ArtifactRenderPlan _service = null!;

    void Because()
    {
        _application = CratisRendering.Plan(_model, _executionPlan, _request.Scope, Options(false));
        _service = CratisRendering.Plan(_model, _executionPlan, _request.Scope, Options(true));
    }

    [Theory]
    [InlineData(false, "Program.cs")]
    [InlineData(false, "GlobalUsings.cs")]
    [InlineData(true, "Program.cs")]
    [InlineData(true, "GlobalUsings.cs")]
    void should_apply_exactly_one_header_block(bool serviceLayout, string relativePath)
    {
        var plan = serviceLayout ? _service : _application;
        plan.Success.ShouldBeTrue();
        var path = (serviceLayout ? "Source/Projects/" : string.Empty) + relativePath;
        var content = Text(plan.Artifacts.Single(artifact => artifact.RelativePath == path));
        content.StartsWith("// Header\n\n", StringComparison.Ordinal).ShouldBeTrue();
        content.Split("// Header\n\n", StringSplitOptions.None).Length.ShouldEqual(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_keep_header_application_idempotent_when_finalizing_again(bool serviceLayout)
    {
        var plan = serviceLayout ? _service : _application;
        var request = _request with { Profile = CratisRendering.CreateProfile(_model.Application.Name, Options(serviceLayout)) };
        var finalized = ArtifactRenderPlan.Create(request, plan.Artifacts, plan.Diagnostics, plan.ImplementationGaps, plan.Refusals, plan.RequiredRemovals);
        finalized.Success.ShouldBeTrue();
        finalized.Artifacts.Select(artifact => artifact.Sha256).ShouldEqual(plan.Artifacts.Select(artifact => artifact.Sha256));
    }

    CratisRenderingOptions Options(bool serviceLayout) => _options with { ServiceLayout = serviceLayout, FileHeader = ["Header"] };
}
