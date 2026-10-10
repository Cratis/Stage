// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_rendering_specification_case_tables : a_multi_slice_application
{
    void Establish() => _application = new ScreenplayCompiler().Compile(a_specification_case_table.Source).Value!;

    async Task Because() => await _renderer.Render([_application], _targetDirectory, _output, _error);

    [Fact] void should_emit_one_file_per_case() => _codeOutput.Files.Where(file => Path.GetFileName(file.RelativePath).StartsWith("when_registering_", StringComparison.Ordinal)).Select(file => Path.GetFileName(file.RelativePath)).ShouldContainOnly("when_registering_small.cs", "when_registering_medium.cs", "when_registering_large.cs");
    [Fact] void should_render_the_small_case_values() => Text("small").ShouldContain("new RegisterProject(\"project-1\", \"small\")");
    [Fact] void should_render_the_medium_case_values() => Text("medium").ShouldContain("@event.Name == \"medium\"");
    [Fact] void should_render_the_large_case_values() => Text("large").ShouldContain("@event.Name == \"large\"");
    [Fact] void should_compile_every_case() => RenderedOutput.Errors(_codeOutput.Files).ShouldBeEmpty();
    [Fact] void should_report_no_rendering_problems() => _error.ToString().ShouldEqual(string.Empty);

    string Text(string name) => _codeOutput.Files.Single(file => Path.GetFileName(file.RelativePath) == $"when_registering_{name}.cs").Content;
}
#endif
