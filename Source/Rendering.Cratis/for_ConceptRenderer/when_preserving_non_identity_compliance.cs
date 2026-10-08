// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_ConceptRenderer.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ConceptRenderer;

public class when_preserving_non_identity_compliance : concepts
{
    [Theory]
    [InlineData("pii")]
    [InlineData("sensitive")]
    public void should_preserve_the_existing_pii_attribute_on_plain_values(string attribute)
    {
        var concept = _emailAddress with { Attributes = [new ConceptAttributeSyntax(attribute, SourceLocation.Start)] };
        var file = ConceptRenderer.Render(concept, _applicationSet, "Generated");
        file.Content.ShouldContain("[PII]");
        file.Content.ShouldContain("ConceptAs<string>");
        file.Content.ShouldNotContain("NotAudited");
        var generated = RenderedOutput.Load([file]).GetTypes().Single(type => type.Name == "EmailAddress");
        generated.GetCustomAttributes(false).ShouldContain(value => value.GetType().Name == "PIIAttribute");
    }
}
