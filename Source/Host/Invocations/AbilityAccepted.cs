// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// The system accepted the work and will answer later, through <see cref="IAbilityInvocations.Answer"/>. The invocation waits without holding a worker.
/// </summary>
public sealed record AbilityAccepted : AbilityResult;
