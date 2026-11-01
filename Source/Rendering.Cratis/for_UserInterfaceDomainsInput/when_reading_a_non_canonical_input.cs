// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_UserInterfaceDomainsInput;

public class when_reading_a_non_canonical_input : Specification
{
    bool _unordered;
    bool _unnormalized;
    bool _shared;
    bool _traversal;
    bool _malformed;
    bool _absent;
    ImmutableDictionary<string, ImmutableArray<string>>? _none;

    void Because()
    {
        _unordered = UserInterfaceDomainsInput.TryRead(Profile("{\"Support\":[\"a\"],\"Sales\":[\"b\"]}"), out _);
        _unnormalized = UserInterfaceDomainsInput.TryRead(Profile("{\"sales\":[\"a\"]}"), out _);
        _shared = UserInterfaceDomainsInput.TryRead(Profile("{\"Sales\":[\"a\"],\"Support\":[\"a\"]}"), out _);
        _traversal = UserInterfaceDomainsInput.TryRead(Profile("{\"Sales/..\":[\"a\"]}"), out _);
        _malformed = UserInterfaceDomainsInput.TryRead(Profile("["), out _);
        _absent = UserInterfaceDomainsInput.TryRead(ArtifactRenderProfile.Create("cratis", "1", "stage", "1", []), out _none);
    }

    [Fact] void should_refuse_unordered_domains() => _unordered.ShouldBeFalse();
    [Fact] void should_refuse_a_domain_that_is_not_normalized() => _unnormalized.ShouldBeFalse();
    [Fact] void should_refuse_a_slice_under_two_domains() => _shared.ShouldBeFalse();
    [Fact] void should_refuse_a_traversing_domain() => _traversal.ShouldBeFalse();
    [Fact] void should_refuse_malformed_json() => _malformed.ShouldBeFalse();
    [Fact] void should_accept_an_absent_input() => _absent.ShouldBeTrue();
    [Fact] void should_read_nothing_when_absent() => _none.ShouldBeNull();

    static ArtifactRenderProfile Profile(string json) => ArtifactRenderProfile.Create(
        "cratis",
        "1",
        "stage",
        "1",
        [ArtifactRenderInput.Create(UserInterfaceDomainsInput.Name, UserInterfaceDomainsInput.Version, [.. System.Text.Encoding.UTF8.GetBytes(json)])]);
}
