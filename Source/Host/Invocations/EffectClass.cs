// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// What retrying an ability invocation can do to the outside world. It decides what happens when an attempt may have reached the system.
/// </summary>
[GenerateSerializer]
[Alias("Cratis.Stage.Host.Invocations.EffectClass")]
public enum EffectClass
{
    /// <summary>
    /// The ability has no lasting effect, so a failed attempt is retried freely.
    /// </summary>
    Safe = 0,

    /// <summary>
    /// The ability has an effect the system deduplicates by the invocation's idempotency key, so a failed attempt is retried with the same key.
    /// </summary>
    Idempotent = 1,

    /// <summary>
    /// The ability has an effect that must happen at most once. An attempt that may have reached the system is never retried: the invocation stops
    /// for an operator to record which outcome happened.
    /// </summary>
    Once = 2
}
