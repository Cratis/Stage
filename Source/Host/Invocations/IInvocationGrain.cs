// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// One ability invocation. The grain key is the invocation identity, so redelivery of the cause finds the invocation that exists.
/// </summary>
[Alias("Cratis.Stage.Host.Invocations.IInvocationGrain")]
public interface IInvocationGrain : IGrainWithStringKey
{
    /// <summary>
    /// Records the invocation with its cause. Recording an invocation that exists changes nothing and returns the existing record.
    /// </summary>
    /// <param name="proposed">The record to write when none exists.</param>
    /// <returns>The record.</returns>
    [Alias("Record")]
    Task<InvocationRecord> Record(InvocationRecord proposed);

    /// <summary>
    /// Gets the record.
    /// </summary>
    /// <returns>The record, or null when nothing was recorded.</returns>
    [Alias("Get")]
    Task<InvocationRecord?> Get();

    /// <summary>
    /// Notes the run that executes the invocation.
    /// </summary>
    /// <param name="run">The workflow run key.</param>
    /// <returns>Awaitable task.</returns>
    [Alias("Started")]
    Task Started(string run);

    /// <summary>
    /// Asks to begin an attempt. For a <see cref="EffectClass.Once"/> invocation the ledger refuses an attempt when an earlier one has begun and has no
    /// outcome, because that one may have reached the system - which is also what a restart in the middle of an attempt looks like.
    /// </summary>
    /// <param name="attempt">The attempt number.</param>
    /// <param name="run">The workflow run key of the run that performs the attempt.</param>
    /// <param name="execution">The node execution key.</param>
    /// <returns>Whether the attempt may call the system.</returns>
    [Alias("BeginAttempt")]
    Task<AttemptStart> BeginAttempt(int attempt, string run, string execution);

    /// <summary>
    /// Notes that the attempt in flight ended without reaching the system or without an outcome that ends the invocation.
    /// </summary>
    /// <param name="reason">Why it ended.</param>
    /// <param name="exhausted">Whether it was the last attempt, so the invocation failed.</param>
    /// <returns>Awaitable task.</returns>
    [Alias("AttemptEnded")]
    Task AttemptEnded(string reason, bool exhausted);

    /// <summary>
    /// Notes that the system accepted the work and the invocation waits for the answer.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    [Alias("Suspended")]
    Task Suspended();

    /// <summary>
    /// Stops the invocation for reconciliation: an attempt may have reached the system and it is not retried.
    /// </summary>
    /// <param name="reason">Why the outcome is unknown.</param>
    /// <returns>Awaitable task.</returns>
    [Alias("Stopped")]
    Task Stopped(string reason);

    /// <summary>
    /// Records who chose the outcome, before the run is taken forward with it, so the choice survives whichever node ends the invocation.
    /// </summary>
    /// <param name="outcome">The outcome chosen.</param>
    /// <param name="output">What the answer carried, as JSON.</param>
    /// <param name="decidedBy">Who answered or reconciled it.</param>
    /// <returns>Awaitable task.</returns>
    [Alias("Decided")]
    Task Decided(string outcome, string output, string decidedBy);

    /// <summary>
    /// Ends the invocation in an outcome.
    /// </summary>
    /// <param name="outcome">The outcome: <c lang="text">returned</c>, a named outcome, or <c lang="text">failed</c>.</param>
    /// <param name="output">What the ability returned, as JSON.</param>
    /// <param name="decidedBy">Who answered or reconciled it, or empty.</param>
    /// <returns>The record.</returns>
    [Alias("Settle")]
    Task<InvocationRecord> Settle(string outcome, string output, string decidedBy);
}
