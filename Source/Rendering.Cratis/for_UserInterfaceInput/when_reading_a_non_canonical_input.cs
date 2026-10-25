// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_UserInterfaceInput;

public class when_reading_a_non_canonical_input : Specification
{
    bool _unordered;
    bool _blank;
    bool _malformed;
    bool _absent;
    ImmutableArray<string>? _none;

    void Because()
    {
        _unordered = UserInterfaceInput.TryRead(Profile("[\"b\",\"a\"]"), out _);
        _blank = UserInterfaceInput.TryRead(Profile("[\" \"]"), out _);
        _malformed = UserInterfaceInput.TryRead(Profile("{"), out _);
        _absent = UserInterfaceInput.TryRead(ArtifactRenderProfile.Create("cratis", "1", "stage", "1", []), out _none);
    }

    [Fact] void should_refuse_unordered_identities() => _unordered.ShouldBeFalse();
    [Fact] void should_refuse_a_blank_identity() => _blank.ShouldBeFalse();
    [Fact] void should_refuse_malformed_json() => _malformed.ShouldBeFalse();
    [Fact] void should_accept_a_profile_without_the_input() => _absent.ShouldBeTrue();
    [Fact] void should_report_no_identities_without_the_input() => _none.ShouldBeNull();
    [Fact] void should_refuse_creating_a_blank_identity() => Catch.Exception(() => UserInterfaceInput.Create([""])).ShouldBeOfExactType<InvalidArtifactRenderContract>();

    static ArtifactRenderProfile Profile(string json) => ArtifactRenderProfile.Create("cratis", "1", "stage", "1", [ArtifactRenderInput.Create(UserInterfaceInput.Name, UserInterfaceInput.Version, [.. System.Text.Encoding.UTF8.GetBytes(json)])]);
}
