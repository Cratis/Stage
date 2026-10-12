// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_ConceptRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ConceptRenderer;

public class when_rendering_a_numbered_enum_concept : concepts
{
    RenderedFile _file = null!;

    void Establish() => _invoiceStatus = _invoiceStatus with { ValueNumbers = [-1, 4, 90] };
    void Because() => _file = ConceptRenderer.Render(_invoiceStatus, _applicationSet, "CratisApp");

    [Fact] void should_preserve_the_negative_member_number() => _file.Content.ShouldContain("Draft = -1,");
    [Fact] void should_preserve_the_sparse_member_number() => _file.Content.ShouldContain("Sent = 4,");
    [Fact] void should_preserve_the_last_member_number() => _file.Content.ShouldContain("Paid = 90,");
}
