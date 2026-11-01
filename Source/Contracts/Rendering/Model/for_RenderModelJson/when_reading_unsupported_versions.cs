// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.Model.for_RenderModelJson;

public class when_reading_unsupported_versions : given.render_models
{
    JsonNode _document = null!;

    void Establish() => _document = JsonNode.Parse(Basic().ToCanonicalJson());

    [Theory]
    [InlineData("schema", "\"stage-render-model/v2\"", "stage-render-model/v1")]
    [InlineData("schema", "\"other-model/v1\"", "stage-render-model/v1")]
    [InlineData("schema", "null", "stage-render-model/v1")]
    [InlineData("schema", "1", "stage-render-model/v1")]
    [InlineData("schema", null, "stage-render-model/v1")]
    [InlineData("canonicalization", "2", "1")]
    [InlineData("canonicalization", "0", "1")]
    [InlineData("canonicalization", "null", "1")]
    [InlineData("canonicalization", "\"1\"", "1")]
    [InlineData("canonicalization", null, "1")]
    void should_refuse_the_version_with_a_clear_exception(string member, string? value, string expected)
    {
        if (value is null) _document.AsObject().Remove(member);
        else _document[member] = JsonNode.Parse(value);

        var exception = Catch.Exception(() => RenderModelJson.Deserialize(JsonSerializer.SerializeToUtf8Bytes(_document)));

        exception.ShouldBeOfExactType<InvalidRenderModel>();
        exception.Message.ShouldContain(member);
        exception.Message.ShouldContain(expected);
    }
}
