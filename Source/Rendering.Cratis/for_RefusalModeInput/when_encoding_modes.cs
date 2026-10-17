// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_RefusalModeInput;

public class when_encoding_modes : Specification
{
    ArtifactRenderInput _refuse = null!;
    ArtifactRenderInput _around = null!;
    void Because()
    {
        _refuse = RefusalModeInput.Create(ArtifactRefusalMode.Refuse);
        _around = RefusalModeInput.Create(ArtifactRefusalMode.RenderAround);
    }

    [Fact] void should_use_the_versioned_input_name() => _around.Name.ShouldEqual("cratis-render:refusals");
    [Fact] void should_use_v1() => _around.Version.ShouldEqual("1");
    [Fact] void should_encode_refuse_without_a_newline() => Encoding.UTF8.GetString(_refuse.Bytes.AsSpan()).ShouldEqual("Refuse");
    [Fact] void should_encode_render_around_without_a_newline() => Encoding.UTF8.GetString(_around.Bytes.AsSpan()).ShouldEqual("RenderAround");
    [Fact] void should_hash_the_mode() => _around.Sha256.ShouldNotEqual(_refuse.Sha256);
    [Fact] void should_round_trip_refuse() => Read(_refuse).ShouldEqual(ArtifactRefusalMode.Refuse);
    [Fact] void should_round_trip_render_around() => Read(_around).ShouldEqual(ArtifactRefusalMode.RenderAround);
    [Fact] void should_be_deterministic() => RefusalModeInput.Create(ArtifactRefusalMode.RenderAround).Sha256.ShouldEqual(_around.Sha256);

    static ArtifactRefusalMode Read(ArtifactRenderInput input)
    {
        RefusalModeInput.TryRead(input, out var mode).ShouldBeTrue();
        return mode;
    }
}
