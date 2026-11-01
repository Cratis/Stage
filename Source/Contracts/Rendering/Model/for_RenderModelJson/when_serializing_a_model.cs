// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.Model.for_RenderModelJson;

public class when_serializing_a_model : given.render_models
{
    byte[] _bytes = [];

    void Because() => _bytes = Basic().ToCanonicalJson();

    [Fact] void should_round_trip_identically() => EqualBytes(_bytes, RenderModelJson.Serialize(RenderModelJson.Deserialize(_bytes)));
    [Fact] void should_not_have_a_bom() => _bytes.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }).ShouldBeFalse();
    [Fact] void should_not_have_a_trailing_newline() => _bytes[^1].ShouldEqual((byte)'}');
    [Fact] void should_emit_independent_versions() => Encoding.UTF8.GetString(_bytes).StartsWith("{\"schema\":\"stage-render-model/v1\",\"canonicalization\":1,\"minor\":0,", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_lose_semantic_identities() => RenderModelJson.Deserialize(_bytes).ShouldEqual(Basic());
    [Fact] void should_use_the_response_discriminator() => Encoding.UTF8.GetString(_bytes).ShouldContain("\"$kind\":\"scalar\"");
    [Fact] void should_match_the_saved_golden_bytes() => EqualBytes(File.ReadAllBytes(for_RenderModelSchema.given.render_model_schema.FilePath("for_RenderModelJson/golden/basic.json")), _bytes);
    [Fact] void should_refuse_unknown_properties() => Catch.Exception(() => RenderModelJson.Deserialize(Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(_bytes).Replace("\"minor\":0", "\"unexpected\":true,\"minor\":0", StringComparison.Ordinal)))).ShouldBeOfExactType<JsonException>();
}
