// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationBuilds;

public class when_resolving_the_limit
{
    [Fact] void should_run_one_process_at_a_time_by_default() => GeneratedApplicationBuilds.Limit(null).ShouldEqual(1);
    [Fact] void should_run_one_process_at_a_time_when_the_value_is_blank() => GeneratedApplicationBuilds.Limit(" ").ShouldEqual(1);
    [Fact] void should_use_the_configured_limit() => GeneratedApplicationBuilds.Limit("3").ShouldEqual(3);
    [Fact] void should_refuse_a_zero_limit() => Catch.Exception(() => GeneratedApplicationBuilds.Limit("0")).ShouldBeOfExactType<ArgumentException>();
    [Fact] void should_refuse_a_limit_that_is_not_a_number() => Catch.Exception(() => GeneratedApplicationBuilds.Limit("many")).ShouldBeOfExactType<ArgumentException>();
}
