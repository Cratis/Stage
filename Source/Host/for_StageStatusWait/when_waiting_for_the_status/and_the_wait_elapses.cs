// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageStatusWait.when_waiting_for_the_status;

public class and_the_wait_elapses : given.a_status_wait
{
    Task Because() => WaitFor(1);

    [Fact] void should_report_loading() => _result.State.ShouldEqual(StageStatusWait.Loading);
    [Fact] void should_wait_for_the_requested_time() => TotalDelay.ShouldEqual(TimeSpan.FromSeconds(1));
    [Fact] void should_read_again_after_every_interval() => _reads.ShouldEqual(11);
}
