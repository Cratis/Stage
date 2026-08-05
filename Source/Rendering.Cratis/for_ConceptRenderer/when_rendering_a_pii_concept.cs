// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_ConceptRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ConceptRenderer;

public class when_rendering_a_pii_concept : concepts
{
    RenderedFile _file = null!;

    void Because() => _file = ConceptRenderer.Render(_emailAddress, _applicationSet, "CratisApp");

    [Fact] void should_emit_the_pii_attribute() => _file.Content.ShouldContain("[PII]");
    [Fact] void should_import_the_pii_namespace() => _file.Content.ShouldContain("using Cratis.Chronicle.Compliance.GDPR;");
}
