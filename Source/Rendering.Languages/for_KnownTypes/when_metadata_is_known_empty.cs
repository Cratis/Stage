// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.for_KnownTypes;

public class when_metadata_is_known_empty : Specification
{
    KnownTypes<string> _metadata;

    void Because() => _metadata = KnownTypes<string>.None;

    [Fact] void should_be_known() => _metadata.IsKnown.ShouldBeTrue();
    [Fact] void should_have_no_values() => _metadata.Values.Count.ShouldEqual(0);
    [Fact] void should_treat_an_explicit_empty_list_as_known_none() => KnownTypes<string>.Of([]).ShouldEqual(_metadata);
    [Fact] void should_be_distinct_from_unknown() => _metadata.ShouldNotEqual(KnownTypes<string>.Unknown);
}
