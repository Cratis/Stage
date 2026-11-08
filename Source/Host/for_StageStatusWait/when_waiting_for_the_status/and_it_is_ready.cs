// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageStatusWait.when_waiting_for_the_status;

public class and_it_is_ready : given.a_status_wait
{
    void Establish() => _stateForRead = _ => "ready";

    Task Because() => WaitFor(120);

    [Fact] void should_report_ready() => _result.State.ShouldEqual("ready");
    [Fact] void should_read_once() => _reads.ShouldEqual(1);
    [Fact] void should_not_wait() => _delays.ShouldBeEmpty();
}
