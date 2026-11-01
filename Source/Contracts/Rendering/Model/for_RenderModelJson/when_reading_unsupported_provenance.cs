// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.Model.for_RenderModelJson;

public class when_reading_unsupported_provenance : given.render_models
{
    JsonNode _document = null!;
    JsonNode _provenance = null!;

    void Establish()
    {
        _document = JsonNode.Parse(Basic().ToCanonicalJson());
        _provenance = _document["items"]["commands"][0]["provenance"];
    }

    [Theory]
    [InlineData("Document")]
    [InlineData("Span")]
    [InlineData("Unknown")]
    [InlineData("absent")]
    void should_refuse_an_unsupported_status(string status)
    {
        _provenance["status"] = status;

        var exception = Catch.Exception(() => RenderModelJson.Deserialize(JsonSerializer.SerializeToUtf8Bytes(_document)));

        exception.ShouldBeOfExactType<InvalidRenderModel>();
        exception.Message.ShouldContain("provenance");
        exception.Message.ShouldContain(status);
    }

    [Theory]
    [InlineData("document")]
    [InlineData("span")]
    void should_refuse_extra_members_even_with_absent_status(string member)
    {
        _provenance[member] = new JsonObject();

        var exception = Catch.Exception(() => RenderModelJson.Deserialize(JsonSerializer.SerializeToUtf8Bytes(_document)));

        exception.ShouldBeOfExactType<JsonException>();
        exception.Message.ShouldContain(member);
    }
}
