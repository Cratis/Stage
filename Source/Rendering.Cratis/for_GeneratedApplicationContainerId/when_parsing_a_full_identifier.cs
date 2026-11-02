// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerId;

public class when_parsing_a_full_identifier : Specification
{
    static readonly string _text = string.Concat(Enumerable.Repeat("0123456789abcdef", 4));
    bool _accepted;
    GeneratedApplicationContainerId _id = null!;

    void Because() => _accepted = GeneratedApplicationContainerId.TryParse(_text, out _id);

    [Fact] void should_accept_it() => _accepted.ShouldBeTrue();
    [Fact] void should_keep_the_value() => _id.Value.ShouldEqual(_text);
}
