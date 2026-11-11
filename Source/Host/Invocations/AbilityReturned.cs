// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// The system did the work.
/// </summary>
/// <param name="Outcome">The named outcome, or null for a plain <c lang="text">returned</c>.</param>
/// <param name="Output">What the ability returned, as JSON.</param>
public sealed record AbilityReturned(string? Outcome = null, string Output = "{}") : AbilityResult;
