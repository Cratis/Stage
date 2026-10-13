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
    [InlineData("Id", "GenerateValue.UuidV7()")]
    [InlineData("Receipt", "GenerateValue.NanoId(21, null)")]
    [InlineData("StringUuid", "\"ref_\" + global::Cratis.Concepts.GenerateValue.Uuid().ToString(\"D\").ToUpperInvariant()")]
    [InlineData("SortableId", "GenerateValue.Ulid().ToLowerInvariant()")]
    [InlineData("UserId", "GenerateValue.TypeId(\"user\")")]
    [InlineData("BareTypeId", "GenerateValue.TypeId(\"\")")]
    [InlineData("BinaryCode", "GenerateValue.NanoId(64, \"ab\")")]
    [InlineData("EncodedRandom", "GenerateValue.Bytes(32, global::Cratis.Concepts.ValueEncoding.Base64Url)")]
    [InlineData("HexReference", "GenerateValue.Bytes(16, global::Cratis.Concepts.ValueEncoding.Hex).ToUpperInvariant()")]
    [InlineData("Base32Reference", "GenerateValue.Bytes(16, global::Cratis.Concepts.ValueEncoding.Base32).ToLowerInvariant()")]
    [InlineData("ImplicitId", "GenerateValue.Uuid()")]
    public void should_emit_the_concepts_strategy_factory(string name, string factory)
    {
        var plan = a_generation_model.Plan();
        plan.Success.ShouldBeTrue();
        Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath == $"Common/{name}.cs").Bytes.AsSpan()).ShouldContain(factory);
    }
}
