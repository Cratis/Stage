// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageStatusWait.when_waiting_for_the_status;

public class and_the_wait_is_over_the_maximum : given.a_status_wait
{
    Task Because() => WaitFor(1000);

    [Fact] void should_report_loading() => _result.State.ShouldEqual(StageStatusWait.Loading);
    [Fact] void should_wait_no_longer_than_the_maximum() => TotalDelay.ShouldEqual(TimeSpan.FromSeconds(StageStatusWait.MaximumSeconds));
}
