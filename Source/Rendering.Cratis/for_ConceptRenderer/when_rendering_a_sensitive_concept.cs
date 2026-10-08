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

public class when_rendering_a_sensitive_concept : concepts
{
    RenderedFile _file = null!;

    void Because() => _file = ConceptRenderer.Render(
        _emailAddress with { Attributes = [new ConceptAttributeSyntax("sensitive", SourceLocation.Start)] },
        _applicationSet,
        "Generated");

    [Fact] void should_encrypt_the_value() => _file.Content.ShouldContain("[Encrypted]");
    [Fact] void should_withhold_the_value_from_causation() => _file.Content.ShouldContain("[NotAudited]");
    [Fact] void should_import_the_encryption_namespace() => _file.Content.ShouldContain("using Cratis.Chronicle.ProtectedValues;");
    [Fact] void should_import_the_audit_namespace() => _file.Content.ShouldContain("using Cratis.Arc.Chronicle.Commands;");
    [Fact] void should_not_enroll_the_value_in_erasure() => _file.Content.ShouldNotContain("[PII]");
    [Fact] void should_compile_the_attributes_on_the_concept() => RenderedOutput.Errors([_file]).ShouldBeEmpty();
}
