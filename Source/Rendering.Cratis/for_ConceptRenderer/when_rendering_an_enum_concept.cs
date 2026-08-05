// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_ConceptRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ConceptRenderer;

public class when_rendering_an_enum_concept : concepts
{
    RenderedFile _file = null!;

    void Because() => _file = ConceptRenderer.Render(_invoiceStatus, _applicationSet, "CratisApp");

    [Fact] void should_emit_an_enum() => _file.Content.ShouldContain("public enum InvoiceStatus");
    [Fact] void should_pascal_case_the_first_member() => _file.Content.ShouldContain("Draft,");
    [Fact] void should_pascal_case_the_second_member() => _file.Content.ShouldContain("Sent,");
    [Fact] void should_pascal_case_the_third_member() => _file.Content.ShouldContain("Paid,");
}
