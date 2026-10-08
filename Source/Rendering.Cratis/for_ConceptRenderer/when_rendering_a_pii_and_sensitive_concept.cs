// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_ConceptRenderer.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ConceptRenderer;

public class when_rendering_a_pii_and_sensitive_concept : concepts
{
    RenderedFile _file = null!;

    void Because() => _file = ConceptRenderer.Render(
        _emailAddress with { Attributes = [new ConceptAttributeSyntax("pii", SourceLocation.Start), new ConceptAttributeSyntax("sensitive", SourceLocation.Start)] },
        _applicationSet,
        "Generated");

    [Fact] void should_preserve_personal_data_protection() => _file.Content.ShouldContain("[PII]");
    [Fact] void should_not_combine_pii_with_plain_encryption() => _file.Content.ShouldNotContain("Encrypted");
    [Fact] void should_rely_on_pii_for_causation_exclusion() => _file.Content.ShouldNotContain("NotAudited");
    [Fact] void should_compile_the_attributes_on_the_concept() => RenderedOutput.Errors([_file]).ShouldBeEmpty();
}
