// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactImplementationGap;

public class when_round_tripping_json : given.a_gap
{
    ArtifactImplementationGap _roundTripped = null!;
    string _json = null!;
    void Establish() => _json = JsonSerializer.Serialize(_gap, StageJson.Options);
    void Because() => _roundTripped = JsonSerializer.Deserialize<ArtifactImplementationGap>(_json, StageJson.Options)!;

    [Fact] void should_preserve_every_payload_field() => JsonSerializer.Serialize(_roundTripped, StageJson.Options).ShouldEqual(_json);
    [Fact] void should_preserve_the_validated_digest() => Digest([_roundTripped]).ShouldEqual(Digest([_gap]));
    [Fact] void should_preserve_unrealized_context_members() => _roundTripped.Stubs[0].TypedContext.Members[1].IsRealized.ShouldBeFalse();
    [Fact] void should_preserve_the_opaque_owner_string() => _roundTripped.Owner.ShouldEqual(_request.Model.Application.Id.ToString());
    [Fact] void should_keep_the_string_fallback_because_stage_json_does_not_encode_semantic_ids_as_strings() => JsonSerializer.Serialize(_request.Model.Application.Id, StageJson.Options)
        .ShouldNotEqual(JsonSerializer.Serialize(_request.Model.Application.Id.ToString(), StageJson.Options));
}
