// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_ConceptRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ConceptRenderer;

public class when_rendering_an_identifier_concept : concepts
{
    RenderedFile _file = null!;

    void Because() => _file = ConceptRenderer.Render(_invoiceId, _applicationSet, "CratisApp");

    [Fact] void should_derive_from_event_source_id() => _file.Content.ShouldContain("public record InvoiceId(Guid Value) : EventSourceId<Guid>(Value)");
    [Fact] void should_declare_a_not_set_sentinel() => _file.Content.ShouldContain("public static readonly InvoiceId NotSet = new(Guid.Empty);");
    [Fact] void should_declare_a_new_factory() => _file.Content.ShouldContain("public static InvoiceId New() => new(Guid.NewGuid());");
    [Fact] void should_not_declare_a_conversion_to_the_primitive() => _file.Content.ShouldNotContain("implicit operator Guid(InvoiceId");
}
