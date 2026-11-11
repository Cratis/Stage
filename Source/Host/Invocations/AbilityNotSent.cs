// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// The call certainly did not reach the system, so nothing happened and it is safe to try again, even for a <see cref="EffectClass.Once"/> ability.
/// </summary>
/// <param name="Reason">Why it did not reach the system.</param>
public sealed record AbilityNotSent(string Reason) : AbilityResult;
