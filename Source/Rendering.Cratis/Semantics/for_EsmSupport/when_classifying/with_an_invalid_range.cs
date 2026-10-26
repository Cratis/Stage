// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_EsmSupport.when_classifying;

public class with_an_invalid_range : Specification
{
    [Fact] void should_refuse_a_nonadjacent_ceiling() => Catch.Exception(() => _ = new EsmVersionRange(new(32, 0), 34)).ShouldBeOfExactType<ArgumentOutOfRangeException>();
    [Fact] void should_refuse_a_default_floor() => Catch.Exception(() => _ = new EsmVersionRange(default, 1)).ShouldBeOfExactType<ArgumentOutOfRangeException>();
    [Fact] void should_refuse_an_overflowed_ceiling() => Catch.Exception(() => _ = new EsmVersionRange(new(uint.MaxValue, 0), 0)).ShouldBeOfExactType<ArgumentOutOfRangeException>();
    [Fact] void should_refuse_an_equal_exclusive_major() => Catch.Exception(() => _ = new EsmVersionRange(new(32, 0), 32)).ShouldBeOfExactType<ArgumentOutOfRangeException>();
    [Fact] void should_refuse_a_reversed_range() => Catch.Exception(() => _ = new EsmVersionRange(new(32, 1), 31)).ShouldBeOfExactType<ArgumentOutOfRangeException>();
}
