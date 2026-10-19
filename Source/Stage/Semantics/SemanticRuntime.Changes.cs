// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Semantics;

internal sealed partial class SemanticRuntime : ISemanticWorldChanges
{
    readonly Lock _subscriptionsLock = new();
    readonly List<Action> _subscriptions = [];

    /// <inheritdoc/>
    public IDisposable Subscribe(Action changed)
    {
        lock (_subscriptionsLock)
        {
            _subscriptions.Add(changed);
        }

        return new Subscription(this, changed);
    }

    // Called after the committed world was replaced: by a settled command or by facts another writer appended.
    void PublishChange()
    {
        Action[] subscribers;
        lock (_subscriptionsLock)
        {
            subscribers = [.. _subscriptions];
        }

        foreach (var subscriber in subscribers)
        {
            subscriber();
        }
    }

    sealed class Subscription(SemanticRuntime runtime, Action changed) : IDisposable
    {
        public void Dispose()
        {
            lock (runtime._subscriptionsLock)
            {
                runtime._subscriptions.Remove(changed);
            }
        }
    }
}
