// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// What an extension backend answers for an <see cref="AbilityCall"/>: <see cref="AbilityReturned"/>, <see cref="AbilityAccepted"/>,
/// <see cref="AbilityNotSent"/> or <see cref="AbilityFailed"/>.
/// </summary>
public abstract record AbilityResult;
