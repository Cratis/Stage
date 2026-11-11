// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// What the ledger answers when an attempt asks to begin.
/// </summary>
[GenerateSerializer]
[Alias("Cratis.Stage.Host.Invocations.AttemptStart")]
public enum AttemptStart
{
    /// <summary>
    /// The attempt may call the system. The ledger recorded that it began before anything was sent.
    /// </summary>
    Begun = 0,

    /// <summary>
    /// A <see cref="EffectClass.Once"/> attempt had already begun and has no outcome: it may have reached the system, so this one must not call it again.
    /// The invocation is stopped for reconciliation.
    /// </summary>
    AlreadyBegun = 1,

    /// <summary>
    /// The invocation has ended or been reconciled, so there is nothing to attempt.
    /// </summary>
    Over = 2
}
