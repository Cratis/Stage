// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationBuilds;

public class when_resolving_the_limit
{
    [Fact] void should_use_a_quarter_of_the_processors_locally() => GeneratedApplicationBuilds.Limit(null, false, 16).ShouldEqual(4);
    [Fact] void should_allow_at_least_one_process_locally() => GeneratedApplicationBuilds.Limit(null, false, 2).ShouldEqual(1);
    [Fact] void should_not_limit_ci() => GeneratedApplicationBuilds.Limit(null, true, 4).ShouldBeNull();
    [Fact] void should_use_the_configured_limit_locally() => GeneratedApplicationBuilds.Limit("3", false, 16).ShouldEqual(3);
    [Fact] void should_use_the_configured_limit_in_ci() => GeneratedApplicationBuilds.Limit("2", true, 16).ShouldEqual(2);
    [Fact] void should_refuse_a_zero_limit() => Catch.Exception(() => GeneratedApplicationBuilds.Limit("0", false, 16)).ShouldBeOfExactType<ArgumentException>();
    [Fact] void should_refuse_a_limit_that_is_not_a_number() => Catch.Exception(() => GeneratedApplicationBuilds.Limit("many", false, 16)).ShouldBeOfExactType<ArgumentException>();
}
