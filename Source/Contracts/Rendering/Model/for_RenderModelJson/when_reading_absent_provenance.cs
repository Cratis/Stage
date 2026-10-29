// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.Model.for_RenderModelJson;

public class when_reading_absent_provenance : given.render_models
{
    byte[] _bytes = [];
    RenderModel _model = null!;

    void Establish()
    {
        var document = JsonNode.Parse(Basic().ToCanonicalJson());
        document["items"]["commands"][0]["provenance"]["reason"] = "source mapping unavailable";
        _bytes = JsonSerializer.SerializeToUtf8Bytes(document);
    }

    void Because() => _model = RenderModelJson.Deserialize(_bytes);

    [Fact] void should_preserve_the_absent_status() => _model.Items.Commands[0].Provenance.Status.ShouldEqual("Absent");
    [Fact] void should_preserve_the_written_reason() => _model.Items.Commands[0].Provenance.Reason.ShouldEqual("source mapping unavailable");
    [Fact] void should_round_trip_without_losing_provenance() => EqualBytes(_bytes, _model.ToCanonicalJson());
}
