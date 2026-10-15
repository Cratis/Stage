// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ImplementationGapInput;

public class when_encoding_modes : Specification
{
    ArtifactRenderInput _refuse = null!;
    ArtifactRenderInput _stub = null!;
    void Because()
    {
        _refuse = ImplementationGapInput.Create(ImplementationGapMode.Refuse);
        _stub = ImplementationGapInput.Create(ImplementationGapMode.Stub);
    }

    [Fact] void should_use_the_versioned_input_name() => _stub.Name.ShouldEqual("cratis-implementation:gaps");
    [Fact] void should_use_v1() => _stub.Version.ShouldEqual("1");
    [Fact] void should_encode_refuse_without_a_newline() => Encoding.UTF8.GetString(_refuse.Bytes.AsSpan()).ShouldEqual("Refuse");
    [Fact] void should_encode_stub_without_a_newline() => Encoding.UTF8.GetString(_stub.Bytes.AsSpan()).ShouldEqual("Stub");
    [Fact] void should_hash_the_mode() => _stub.Sha256.ShouldNotEqual(_refuse.Sha256);
    [Fact] void should_round_trip_refuse() => Read(_refuse).ShouldEqual(ImplementationGapMode.Refuse);
    [Fact] void should_round_trip_stub() => Read(_stub).ShouldEqual(ImplementationGapMode.Stub);
    [Fact] void should_be_deterministic() => ImplementationGapInput.Create(ImplementationGapMode.Stub).Sha256.ShouldEqual(_stub.Sha256);

    static ImplementationGapMode Read(ArtifactRenderInput input)
    {
        ImplementationGapInput.TryRead(input, out var mode).ShouldBeTrue();
        return mode;
    }
}
