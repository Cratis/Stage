// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageStatusWait.when_waiting_for_the_status;

public class and_the_request_is_aborted : given.a_status_wait
{
    readonly CancellationTokenSource _aborted = new();

    Task Because() => WaitFor(120, _aborted.Token);

    void Destroy() => _aborted.Dispose();

    protected override Task Delay(TimeSpan delay, CancellationToken cancellationToken)
    {
        _delays.Add(delay);
        _aborted.Cancel();
        return Task.FromCanceled(cancellationToken);
    }

    [Fact] void should_report_the_last_status() => _result.State.ShouldEqual(StageStatusWait.Loading);
    [Fact] void should_stop_waiting() => _delays.Count.ShouldEqual(1);
    [Fact] void should_not_read_again() => _reads.ShouldEqual(1);
}
