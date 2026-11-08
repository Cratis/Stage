// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageStatusWait.when_waiting_for_the_status;

public class without_a_wait : given.a_status_wait
{
    Task Because() => WaitFor(null);

    [Fact] void should_report_loading() => _result.State.ShouldEqual(StageStatusWait.Loading);
    [Fact] void should_read_once() => _reads.ShouldEqual(1);
    [Fact] void should_not_wait() => _delays.ShouldBeEmpty();
}
