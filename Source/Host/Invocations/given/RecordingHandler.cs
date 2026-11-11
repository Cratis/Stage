// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Stage.Host.Invocations.given;

public sealed class RecordingHandler(InvocationOriginKind kind) : IInvocationOriginHandler
{
    public ConcurrentQueue<InvocationRecord> Settlements { get; } = new();

    public InvocationOriginKind Kind { get; } = kind;

    public Task Settled(InvocationRecord record)
    {
        Settlements.Enqueue(record);
        return Task.CompletedTask;
    }
}
