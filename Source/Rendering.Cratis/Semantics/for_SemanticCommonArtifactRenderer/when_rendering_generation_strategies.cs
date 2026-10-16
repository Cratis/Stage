// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCommonArtifactRenderer;

public class when_rendering_generation_strategies : Specification
{
    [Theory]
    [InlineData("Id", "GenerateValue.UuidV7(context)")]
    [InlineData("Receipt", "GenerateValue.NanoId(21, null)")]
    [InlineData("StringUuid", "\"ref_\" + GenerateValue.Uuid().ToString(\"D\").ToUpperInvariant()")]
    [InlineData("SortableId", "GenerateValue.Ulid(context).ToLowerInvariant()")]
    [InlineData("UserId", "GenerateValue.TypeId(\"user\", context)")]
    [InlineData("BareTypeId", "GenerateValue.TypeId(\"\", context)")]
    [InlineData("BinaryCode", "GenerateValue.NanoId(64, \"ab\")")]
    [InlineData("EncodedRandom", "GenerateValue.Bytes(32, ValueEncoding.Base64Url)")]
    [InlineData("HexReference", "GenerateValue.Bytes(16, ValueEncoding.Hex).ToUpperInvariant()")]
    [InlineData("Base32Reference", "GenerateValue.Bytes(16, ValueEncoding.Base32).ToLowerInvariant()")]
    [InlineData("ImplicitId", "GenerateValue.Uuid()")]
    public void should_emit_the_concepts_strategy_factory(string name, string factory)
    {
        var plan = a_generation_model.Plan();
        plan.Success.ShouldBeTrue();
        var source = Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath == $"Common/{name}.cs").Bytes.AsSpan());
        source.ShouldContain(factory);
        var usesContext = name is "Id" or "SortableId" or "UserId" or "BareTypeId";
        source.ShouldContain($"{(usesContext ? "IGeneratableInContext" : "IGeneratable")}<{name}>");
        source.ShouldContain($"New({(usesContext ? "GenerationContext context" : string.Empty)})");
        if (usesContext) source.ShouldNotContain("New()");
    }
}
