// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// One attempt at an ability, as the extension backend receives it.
/// </summary>
/// <param name="Ability">The ability and its contract major.</param>
/// <param name="Effect">The <see cref="EffectClass"/>, so the extension knows whether it will be called again.</param>
/// <param name="IdempotencyKey">The invocation identity, the same for every attempt. The extension deduplicates its effect on it (<c lang="text">$invocation.idempotencyKey</c>).</param>
/// <param name="Attempt">The attempt number, starting at 1.</param>
/// <param name="Input">The ability's input, as JSON.</param>
/// <param name="PackageDigest">The locked digest of the extension package the invocation pinned.</param>
/// <param name="Timeout">How long the attempt may take. It becomes the deadline the extension is told. Zero means none is imposed.</param>
/// <param name="Tenant">The tenant the invocation runs for.</param>
/// <param name="Origin">Where the invocation came from, which the extension's telemetry is tied to.</param>
public sealed record AbilityCall(
    AbilityReference Ability,
    EffectClass Effect,
    string IdempotencyKey,
    int Attempt,
    string Input,
    string PackageDigest,
    TimeSpan Timeout = default,
    string Tenant = "Default",
    InvocationOrigin? Origin = null);
