// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle.Events;

namespace Cratis.Stage.Runtime.for_RootStringKeyContract;

/// <summary>
/// Carries a status for the native constant-key registration specs only.
/// </summary>
/// <param name="Status">The status projected by the fixture read models.</param>
[EventType]
public record RootStringKeyContractUpdated(string Status);
#endif
