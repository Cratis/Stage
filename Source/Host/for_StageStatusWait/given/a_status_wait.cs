// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;

namespace Cratis.Stage.Host.for_StageStatusWait.given;

public class a_status_wait : Specification
{
    protected readonly List<TimeSpan> _delays = [];
    protected Func<int, string> _stateForRead = _ => StageStatusWait.Loading;
    protected int _reads;
    protected StageStatus _result = null!;

    protected TimeSpan TotalDelay => _delays.Aggregate(TimeSpan.Zero, (total, delay) => total + delay);

    protected async Task WaitFor(int? seconds, CancellationToken cancellationToken = default) =>
        _result = await StageStatusWait.Until(Read, seconds, Delay, cancellationToken);

    protected virtual Task Delay(TimeSpan delay, CancellationToken cancellationToken)
    {
        _delays.Add(delay);
        return Task.CompletedTask;
    }

    StageStatus Read() => new(_stateForRead(++_reads), null, null);
}
