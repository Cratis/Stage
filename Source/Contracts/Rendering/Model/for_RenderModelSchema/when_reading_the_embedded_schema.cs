// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.Model.for_RenderModelSchema;

public class when_reading_the_embedded_schema : Specification
{
    const string ResourceName = "Cratis.Stage.Contracts.Rendering.Model.render-model.v1.schema.json";
    byte[]? _embedded;

    void Because()
    {
        using var stream = typeof(RenderModel).Assembly.GetManifestResourceStream(ResourceName);
        if (stream is null) return;
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        _embedded = buffer.ToArray();
    }

    [Fact] void should_ship_the_schema_with_the_assembly() => _embedded.ShouldNotBeNull();

    [Fact] void should_embed_the_saved_schema_bytes() => for_RenderModelJson.given.render_models.EqualBytes(File.ReadAllBytes(given.render_model_schema.FilePath("render-model.v1.schema.json")), _embedded!);
}
