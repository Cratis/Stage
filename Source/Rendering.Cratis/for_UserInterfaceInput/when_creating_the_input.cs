// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_UserInterfaceInput;

public class when_creating_the_input : Specification
{
    ArtifactRenderInput _input = null!;
    ArtifactRenderInput _reordered = null!;
    bool _read;
    ImmutableArray<string>? _slices;

    void Because()
    {
        _input = UserInterfaceInput.Create(["b", "a", "b"]);
        _reordered = UserInterfaceInput.Create(["a", "b"]);
        _read = UserInterfaceInput.TryRead(ArtifactRenderProfile.Create("cratis", "1", "stage", "1", [_input]), out _slices);
    }

    [Fact] void should_use_the_stable_name() => _input.Name.ShouldEqual("cratis-render:user-interface");
    [Fact] void should_use_the_first_version() => _input.Version.ShouldEqual("1");
    [Fact] void should_write_distinct_identities_in_ordinal_order() => System.Text.Encoding.UTF8.GetString(_input.Bytes.AsSpan()).ShouldEqual("[\"a\",\"b\"]");
    [Fact] void should_not_depend_on_the_order_given() => _input.Bytes.SequenceEqual(_reordered.Bytes).ShouldBeTrue();
    [Fact] void should_read_back() => _read.ShouldBeTrue();
    [Fact] void should_read_the_identities() => string.Join(',', _slices!.Value).ShouldEqual("a,b");
}
