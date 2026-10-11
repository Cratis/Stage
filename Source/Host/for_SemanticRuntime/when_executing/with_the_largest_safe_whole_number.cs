// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.when_executing.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class with_the_largest_safe_whole_number : a_whole_number_route
{
    const long Count = 9007199254740991L;

    Task Because() => Record(Count);

    [Fact] void should_accept_the_command() => _result.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_keep_the_largest_whole_number_in_the_fact() => ((SemanticNumberValue)_appended!.Values.Single().Value).Value.ShouldEqual(Count);
    [Fact] void should_route_to_the_exact_stream_id() => _appended!.Route!.StreamId.ShouldEqual(Count.ToString(CultureInfo.InvariantCulture));
}
