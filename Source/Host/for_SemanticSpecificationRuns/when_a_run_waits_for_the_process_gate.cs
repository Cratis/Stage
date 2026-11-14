// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticSpecificationRuns.given;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSpecificationRuns;

public class when_a_run_waits_for_the_process_gate : a_specification_endpoint
{
    readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _calls;
    int _waitingStatus;
    bool _waitingRunStarted;
    bool _waitingTokenCancelled;
    private protected override TimeSpan Timeout => TimeSpan.FromMilliseconds(300);
    private protected override ISpecificationRunProcess Process { get; } = Substitute.For<ISpecificationRunProcess>();

    void Establish() => Process.Run(Arg.Any<ProcessStartInfo>(), Arg.Any<CancellationToken>()).Returns(async call =>
    {
        if (Interlocked.Increment(ref _calls) == 1)
        {
            _started.SetResult();
            await _release.Task;
        }
        else
        {
            _waitingRunStarted = true;
            _waitingTokenCancelled = call.Arg<CancellationToken>().IsCancellationRequested;
        }
        return new SpecificationProcessResult(0, string.Empty, string.Empty);
    });

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        var holding = Request("{}", cancellation.Token);
        Task<int>? waiting = null;
        try
        {
            // Other classes share the gate, so reaching the runner has no separate queue deadline.
            if (await Task.WhenAny(_started.Task, holding) == holding)
            {
                await holding;
                throw new InvalidOperationException("The holding request completed before reaching the runner.");
            }
            waiting = RequestStatus("{}", cancellation.Token);
            // The runner double is slow on purpose: queue time must exceed its execution budget.
            await Task.Delay(Timeout * 3);
            _release.TrySetResult();
            await holding;
            _waitingStatus = await waiting;
        }
        finally
        {
            // Cover startup failure too: a late holder must never strand the shared process gate.
            _release.TrySetResult();
            await cancellation.CancelAsync();
            await Task.WhenAll(holding, waiting ?? Task.CompletedTask).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }

    [Fact] void should_start_the_waiting_runner() => _waitingRunStarted.ShouldBeTrue();
    [Fact] void should_give_the_waiting_runner_a_fresh_budget() => _waitingTokenCancelled.ShouldBeFalse();
    [Fact] void should_not_time_out_while_queueing() => _waitingStatus.ShouldNotEqual(StatusCodes.Status504GatewayTimeout);
}
