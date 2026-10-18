// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Semantics;

namespace Cratis.Stage.Host.for_SemanticLiveQuery.given;

internal sealed class a_fake_world : ISemanticWorldChanges
{
    Action? _changed;

    internal int Subscriptions { get; private set; }

    internal int Disposals { get; private set; }

    internal void Raise() => _changed?.Invoke();

    public IDisposable Subscribe(Action changed)
    {
        Subscriptions++;
        _changed = changed;

        return new Handle(this);
    }

    sealed class Handle(a_fake_world world) : IDisposable
    {
        public void Dispose() => world.Disposals++;
    }
}
