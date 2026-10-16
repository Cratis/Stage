// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticValidationRendering;

public class when_rendering_checksum_rules
{
    [Theory]
    [InlineData("luhn", "Luhn")]
    [InlineData("mod11", "Mod11")]
    [InlineData("mod97", "Mod97")]
    public void should_render_the_fundamentals_predicate(string algorithm, string member)
    {
        var rule = invoice_model.Compile($"concept Reference : String\n  validate checksum {algorithm}").Application.Concepts.Single().Validations.Single();
        var builder = new CSharpCodeBuilder();
        SemanticValidationRendering.CanRender(rule).ShouldBeTrue();
        SemanticValidationRendering.Render(builder, rule, "reference", SemanticPrimitiveType.Text, collection: false, concept: true);
        builder.ToString().ShouldContain($"value is null || global::Cratis.Concepts.GenerateValue.HasValidChecksum(value, global::Cratis.Concepts.Checksum.{member})");
    }
}
