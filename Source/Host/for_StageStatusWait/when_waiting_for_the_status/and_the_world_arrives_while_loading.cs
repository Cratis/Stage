// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageStatusWait.when_waiting_for_the_status;

public class and_the_world_arrives_while_loading : given.a_status_wait
{
    void Establish() => _stateForRead = read => read > 3 ? "ready" : StageStatusWait.Loading;

    Task Because() => WaitFor(120);

    [Fact] void should_report_ready() => _result.State.ShouldEqual("ready");
    [Fact] void should_stop_reading_once_settled() => _reads.ShouldEqual(4);
    [Fact] void should_wait_between_reads() => _delays.ShouldContainOnly([StageStatusWait.Interval, StageStatusWait.Interval, StageStatusWait.Interval]);
}
