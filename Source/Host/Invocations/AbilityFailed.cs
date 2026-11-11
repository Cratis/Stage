// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// The call failed. A backend that cannot rule out that the call reached the system says so, which is what an exception or a timeout means.
/// </summary>
/// <param name="Reason">What went wrong.</param>
/// <param name="MayHaveReachedTheSystem">Whether the effect may have happened.</param>
public sealed record AbilityFailed(string Reason, bool MayHaveReachedTheSystem = true) : AbilityResult;
