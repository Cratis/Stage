// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_rendering_specification_examples_without_output_scopes : an_application_with_specification_examples
{
    ApplicationSet _context = null!;
    LocatedSlice _slice = null!;
    string _wholeApplicationSpecification = null!;

    async Task Establish()
    {
        _context = new ApplicationSet([_application]);
        _slice = _context.Slices.Single();
        await _renderer.Render([_application], _targetDirectory, _output, _error);
        _wholeApplicationSpecification = SpecificationBody(_codeOutput.Files.Single(file => file.RelativePath.EndsWith("when_registering_aproject.cs", StringComparison.Ordinal)).Content);
    }

    async Task Because() => await _renderer.Render(_slice.Slice, _context, _targetDirectory, _output, _error);

    [Fact] void should_emit_the_specification_under_the_slice_only() => _codeOutput.Files.Count(file => file.RelativePath == SpecificationPath).ShouldEqual(1);
    [Fact] void should_expand_the_same_specification_as_the_whole_application() => SpecificationBody(_codeOutput.Files.Single(file => file.RelativePath == SpecificationPath).Content).ShouldEqual(_wholeApplicationSpecification);
    [Fact] void should_not_report_an_unresolved_example_or_command() => _error.ToString().ShouldBeEmpty();

    static string SpecificationPath => Path.Combine("Register", "when_registering_aproject.cs");

    // Scoped rendering changes imports and namespaces, not the expanded scenario or its assertions.
    static string SpecificationBody(string content) => content[content.IndexOf("public class ", StringComparison.Ordinal)..];
}
