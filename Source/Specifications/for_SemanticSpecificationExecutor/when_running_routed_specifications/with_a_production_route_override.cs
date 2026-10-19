// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_routed_specifications;

public class with_a_production_route_override : a_production_routed_plan
{
    async Task Because() => await Run(_plan);

    [Fact] void should_pass_like_the_reference() => AssertParity(true);
    [Fact] void should_trace_both_facts() => _result.Trace!.Facts.Count.ShouldEqual(2);
    [Fact] void should_keep_the_command_route_on_the_default_production() => _result.Trace!.Facts[0].Route!.StreamKind.ShouldEqual("stored-transactions");
    [Fact] void should_route_the_override_to_its_own_stream() => _result.Trace!.Facts[1].Route!.StreamKind.ShouldEqual("stored-archive");
    [Fact] void should_use_the_override_stream_id() => _result.Trace!.Facts[1].Route!.StreamId.ShouldEqual("archive-1");
}
