// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisPlanOptions.when_writing_file_headers;

public class with_core_inputs : a_register_project_render_request
{
    [Theory]
    [InlineData("stage:file-header")]
    [InlineData("cratis-render:file-header")]
    [InlineData("axon-render:file-header")]
    void should_apply_every_name_through_both_paths(string name)
    {
        var input = ArtifactFileHeader.CreateInput(["Acme"]);
        var profile = Add(ArtifactRenderInput.Create(name, "1", input.Bytes));
        var scaffold = CratisRendering.PlanScaffold(new(_model.Application.Name, "Projects", "Projects") { Profile = profile });
        var application = _planner.Plan(_request with { Profile = profile });
        scaffold.Success.ShouldBeTrue();
        application.Success.ShouldBeTrue();
        Text(scaffold.Artifacts.Single(artifact => artifact.RelativePath == "Program.cs")).StartsWith("// Acme\n\n", StringComparison.Ordinal).ShouldBeTrue();
        Text(application.Artifacts.Single(artifact => artifact.RelativePath == "Program.cs")).StartsWith("// Acme\n\n", StringComparison.Ordinal).ShouldBeTrue();
    }

    [Fact] void should_report_malformed_headers_from_the_core()
    {
        var profile = Add(ArtifactRenderInput.Create(ArtifactFileHeader.InputName, "1", [.. Encoding.UTF8.GetBytes("not json")]));
        var scaffold = CratisRendering.PlanScaffold(new(_model.Application.Name, "Projects", "Projects") { Profile = profile });
        var application = _planner.Plan(_request with { Profile = profile });
        scaffold.Diagnostics.Single().Code.ShouldEqual("STAGE-HEADER-001");
        application.Diagnostics.Single().Code.ShouldEqual("STAGE-HEADER-001");
        scaffold.Artifacts.ShouldBeEmpty();
        application.Artifacts.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("\u2028")]
    [InlineData("\uE000")]
    void should_refuse_unsafe_options(string line) => CratisRendering.PlanScaffold(new(_model.Application.Name, "Projects", "Projects") { FileHeader = [line] }).Diagnostics.Single().Code.ShouldEqual("STAGE-PLAN-023");

    [Fact] void should_preserve_the_legacy_api()
    {
        ArtifactFileHeader.CreateInput(["Ærlig ©", ""]).Bytes.SequenceEqual(FileHeaderInput.Create(["Ærlig ©", ""]).Bytes).ShouldBeTrue();
        FileHeaderInput.Name.ShouldEqual("cratis-render:file-header");
        FileHeaderInput.Version.ShouldEqual("1");
        FileHeaderInput.Create(["\uE000"]).Name.ShouldEqual(FileHeaderInput.Name);
        Encoding.UTF8.GetString(FileHeaderInput.Create(["\uE000"]).Bytes.AsSpan()).ShouldEqual("[\"\\uE000\"]");
        Catch.Exception(() => FileHeaderInput.Create([])).ShouldBeOfExactType<InvalidArtifactRenderContract>();
        Catch.Exception(() => FileHeaderInput.Create(["bad\nline"])).Message.ShouldEqual("A file header input requires one or more single-line, non-null lines.");
    }

    ArtifactRenderProfile Add(ArtifactRenderInput input) => ArtifactRenderProfile.Create(_request.Profile.Target, _request.Profile.TargetVersion, _request.Profile.Renderer, _request.Profile.RendererVersion, [.. _request.Profile.Inputs, input]);
}
