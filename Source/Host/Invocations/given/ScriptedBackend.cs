// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Stage.Host.Invocations.given;

public sealed class ScriptedBackend : IAbilityBackend
{
    public ConcurrentQueue<AbilityCall> Calls { get; } = new();

    public Func<AbilityCall, Task<AbilityResult>> Behavior { get; set; } = _ => Task.FromResult<AbilityResult>(new AbilityReturned());

    public Task<AbilityResult> Invoke(AbilityCall call, CancellationToken cancellationToken)
    {
        Calls.Enqueue(call);
        return Behavior(call);
    }
}
