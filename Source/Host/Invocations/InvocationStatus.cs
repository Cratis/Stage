// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// Where an ability invocation stands.
/// </summary>
[GenerateSerializer]
[Alias("Cratis.Stage.Host.Invocations.InvocationStatus")]
public enum InvocationStatus
{
    /// <summary>
    /// Recorded with its cause, and not yet attempted.
    /// </summary>
    Recorded = 0,

    /// <summary>
    /// An attempt has begun and has no outcome yet.
    /// </summary>
    Running = 1,

    /// <summary>
    /// An attempt failed and another will follow.
    /// </summary>
    Retrying = 2,

    /// <summary>
    /// The system accepted the work and the invocation waits for the answer.
    /// </summary>
    Suspended = 3,

    /// <summary>
    /// The invocation ended in an outcome, which is in <see cref="InvocationRecord.Outcome"/>.
    /// </summary>
    Settled = 4,

    /// <summary>
    /// The invocation used up its attempts, or the system answered that it failed.
    /// </summary>
    Failed = 5,

    /// <summary>
    /// An attempt may have reached the system and the effect is <see cref="EffectClass.Once"/>, so nothing was retried: an operator has to record which outcome happened.
    /// </summary>
    Stopped = 6
}
