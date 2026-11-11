// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography;

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// The grain of one ability invocation. Its state is the outbox of the invocation and the guard that keeps a <see cref="EffectClass.Once"/> effect from repeating.
/// </summary>
/// <param name="state">The persisted state.</param>
public sealed class InvocationGrain([PersistentState("invocation", WellKnownGrainStorageProviders.Workflows)] IPersistentState<InvocationState> state) : Grain, IInvocationGrain
{
    InvocationRecord Current => state.State.Record ?? throw new InvalidOperationException($"The invocation {this.GetPrimaryKeyString()} was never recorded.");

    /// <inheritdoc/>
    public async Task<InvocationRecord> Record(InvocationRecord proposed)
    {
        if (state.State.Record is { } existing) return existing;
        state.State.Record = proposed;
        await state.WriteStateAsync();
        return proposed;
    }

    /// <inheritdoc/>
    public Task<InvocationRecord?> Get() => Task.FromResult(state.State.Record);

    /// <inheritdoc/>
    public Task Started(string run) => Change(record => record.Run.Length == 0 ? record with { Run = run } : record);

    /// <inheritdoc/>
    public async Task<AttemptStart> BeginAttempt(int attempt, string run, string execution)
    {
        var record = Current;
        if (record.Status is InvocationStatus.Settled or InvocationStatus.Failed) return AttemptStart.Over;
        if (record.Effect == EffectClass.Once && record.AttemptInFlight > 0)
        {
            var reason = $"Attempt {record.AttemptInFlight} began and has no recorded outcome, so it may have reached the system. Attempt {attempt} was not made, because the effect happens at most once.";
            await Write(record with { Status = InvocationStatus.Stopped, Reason = reason, Run = Keep(record.Run, run), Execution = execution });
            return AttemptStart.AlreadyBegun;
        }

        await Write(record with
        {
            Status = InvocationStatus.Running,
            Attempts = Math.Max(record.Attempts, attempt),
            AttemptInFlight = attempt,
            Run = Keep(record.Run, run),
            Execution = execution
        });
        return AttemptStart.Begun;
    }

    /// <inheritdoc/>
    public Task AttemptEnded(string reason, bool exhausted) => Change(record => record with
    {
        AttemptInFlight = 0,
        Reason = reason,
        Status = exhausted ? InvocationStatus.Failed : InvocationStatus.Retrying,
        Outcome = exhausted ? AbilityDescriptor.FailedOutlet : record.Outcome,
        Settled = exhausted ? DateTimeOffset.UtcNow : record.Settled
    });

    /// <inheritdoc/>
    public Task Suspended() => Change(record => record with { AttemptInFlight = 0, Status = InvocationStatus.Suspended });

    /// <inheritdoc/>
    public Task Stopped(string reason) => Change(record => record with { Status = InvocationStatus.Stopped, Reason = reason });

    /// <inheritdoc/>
    public async Task<InvocationRecord> Settle(string outcome, string output, string decidedBy)
    {
        var record = Current;
        if (record.Status is InvocationStatus.Settled or InvocationStatus.Failed) return record;
        var failed = outcome == AbilityDescriptor.FailedOutlet;
        var settled = record with
        {
            AttemptInFlight = 0,
            Status = failed ? InvocationStatus.Failed : InvocationStatus.Settled,
            Outcome = outcome,
            Output = output.Length > 0 ? output : record.Output,
            DecidedBy = decidedBy.Length > 0 ? decidedBy : record.DecidedBy,
            Settled = DateTimeOffset.UtcNow
        };
        await Write(settled);
        return settled;
    }

    /// <inheritdoc/>
    public Task Decided(string outcome, string output, string decidedBy) =>
        Change(record => record with { Outcome = outcome, Output = output, DecidedBy = decidedBy });

    static string Keep(string current, string proposed) => current.Length == 0 ? proposed : current;

    Task Change(Func<InvocationRecord, InvocationRecord> change) => Write(change(Current));

    async Task Write(InvocationRecord record)
    {
        state.State.Record = record;
        await state.WriteStateAsync();
    }
}

/// <summary>
/// The persisted state of an <see cref="InvocationGrain"/>.
/// </summary>
[GenerateSerializer]
[Alias("Cratis.Stage.Host.Invocations.InvocationState")]
public sealed class InvocationState
{
    /// <summary>
    /// Gets or sets the record, or null when nothing was recorded.
    /// </summary>
    [Id(0)]
    public InvocationRecord? Record { get; set; }
}
