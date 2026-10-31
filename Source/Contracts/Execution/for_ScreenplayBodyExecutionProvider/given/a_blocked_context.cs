// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Stage.Contracts.Execution.for_ScreenplayBodyExecutionProvider.given;

internal sealed class a_blocked_context : SynchronizationContext
{
    readonly ConcurrentQueue<(SendOrPostCallback Callback, object? State)> _pending = new();

    public override void Post(SendOrPostCallback d, object? state) => _pending.Enqueue((d, state));
}
