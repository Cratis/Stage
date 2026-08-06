// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_ConceptRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ConceptRenderer;

public class when_rendering_a_plain_concept : concepts
{
    RenderedFile _file = null!;

    void Because() => _file = ConceptRenderer.Render(_money, _applicationSet, "CratisApp");

    [Fact] void should_place_the_file_under_common() => _file.RelativePath.ShouldEqual(Path.Combine("Common", "Money.cs"));
    [Fact] void should_derive_from_concept_as() => _file.Content.ShouldContain("public record Money(decimal Value) : ConceptAs<decimal>(Value)");
    [Fact] void should_declare_a_not_set_sentinel_typed_to_the_primitive() =>
        _file.Content.ShouldContain("public static readonly Money NotSet = new(0m);");
    [Fact] void should_declare_a_conversion_to_the_primitive() => _file.Content.ShouldContain("public static implicit operator decimal(Money value) => value.Value;");
    [Fact] void should_declare_a_conversion_from_the_primitive() => _file.Content.ShouldContain("public static implicit operator Money(decimal value) => new(value);");
    [Fact] void should_not_emit_a_validator() => _file.Content.ShouldNotContain("MoneyValidator");
}
