// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_rendering_specification_examples : an_application_with_specification_examples
{
    async Task Because() => await _renderer.Render([_application], _targetDirectory, _output, _error);

    [Fact] void should_emit_the_specification_instead_of_skipping_the_example() => _codeOutput.Files.Count(file => file.RelativePath.EndsWith("when_registering_aproject.cs", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_include_the_effective_command_values() => SpecificationText.ShouldContain("new RegisterProject(\"project-1\", \"changed\")");
    [Fact] void should_include_the_effective_event_values() => SpecificationText.ShouldContain("@event.Name == \"changed\"");
    [Fact] void should_not_report_an_unresolved_example() => _error.ToString().ShouldNotContain("which this slice does not declare");
    [Fact] void should_compile_the_rendered_specification() => RenderedOutput.Errors(_codeOutput.Files).ShouldBeEmpty();

    string SpecificationText => _codeOutput.Files.Single(file => file.RelativePath.EndsWith("when_registering_aproject.cs", StringComparison.Ordinal)).Content;
}
