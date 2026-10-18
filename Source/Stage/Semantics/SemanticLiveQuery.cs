// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;

namespace Cratis.Stage.Semantics;

/// <summary>
/// The result of an <c language="csharp">observable</c> query: the current answer, then a new answer whenever the
/// committed world changes it.
/// </summary>
/// <remarks>
/// <para>
/// Arc recognizes an observable query by the <see cref="ISubject{T}"/> a performer returns and streams every value
/// the subject emits. The world has no per-query change feed, so a change of the world re-reads the query and emits
/// only when the answer differs from the one last emitted: a command that touches another read model leaves this
/// query silent.
/// </para>
/// <para>
/// The subject listens to the world only while something observes it, so an abandoned subscription releases it.
/// </para>
/// </remarks>
/// <typeparam name="T">The type the query answers with.</typeparam>
/// <param name="changes">The changes of the world the query reads.</param>
/// <param name="read">Reads the current answer.</param>
/// <param name="initial">The answer at the time the query was performed.</param>
internal sealed class SemanticLiveQuery<T>(ISemanticWorldChanges changes, Func<Task<T>> read, T initial) : ISubject<T>
{
    readonly Lock _lock = new();
    readonly List<IObserver<T>> _observers = [];
    IDisposable? _listening;
    T _current = initial;
    string _currentText = Text(initial);
    bool _refreshing;
    bool _stale;
    bool _ended;
    Exception? _failure;

    /// <summary>
    /// Gets the answer last emitted. Arc reads it to answer a plain request, which does not wait for the stream.
    /// </summary>
    public T Value
    {
        get
        {
            lock (_lock)
            {
                return _current;
            }
        }
    }

    /// <inheritdoc/>
    public IDisposable Subscribe(IObserver<T> observer)
    {
        var listen = false;
        lock (_lock)
        {
            if (_ended)
            {
                End(observer);
                return NoSubscription.Instance;
            }

            _observers.Add(observer);
            listen = _listening is null;
            observer.OnNext(_current);
        }

        if (listen)
        {
            lock (_lock)
            {
                _listening ??= changes.Subscribe(Changed);
            }

            // The world may have moved between answering the query and the first observer arriving.
            Changed();
        }

        return new Observation(this, observer);
    }

    /// <inheritdoc/>
    public void OnNext(T value)
    {
    }

    /// <inheritdoc/>
    public void OnError(Exception error) => Fail(error);

    /// <inheritdoc/>
    public void OnCompleted() => Fail(null);

    static string Text(T value) => JsonSerializer.Serialize(value);

    static void Emit(IObserver<T> observer, Exception? failure)
    {
        if (failure is null)
        {
            observer.OnCompleted();
        }
        else
        {
            observer.OnError(failure);
        }
    }

    void End(IObserver<T> observer) => Emit(observer, _failure);

    void Changed()
    {
        lock (_lock)
        {
            if (_ended)
            {
                return;
            }

            if (_refreshing)
            {
                _stale = true;
                return;
            }

            _refreshing = true;
        }

        _ = Task.Run(Refresh);
    }

    async Task Refresh()
    {
        while (true)
        {
            T value;
            try
            {
                value = await read();
            }
            catch (Exception error)
            {
                Fail(error);
                return;
            }

            lock (_lock)
            {
                if (_ended)
                {
                    return;
                }

                var text = Text(value);
                if (!string.Equals(text, _currentText, StringComparison.Ordinal))
                {
                    _current = value;
                    _currentText = text;
                    foreach (var observer in _observers.ToArray())
                    {
                        observer.OnNext(value);
                    }
                }

                if (!_stale)
                {
                    _refreshing = false;
                    return;
                }

                _stale = false;
            }
        }
    }

    void Fail(Exception? failure)
    {
        IObserver<T>[] observers;
        IDisposable? listening;
        lock (_lock)
        {
            if (_ended)
            {
                return;
            }

            _ended = true;
            _failure = failure;
            observers = [.. _observers];
            _observers.Clear();
            listening = _listening;
            _listening = null;
        }

        listening?.Dispose();
        foreach (var observer in observers)
        {
            Emit(observer, failure);
        }
    }

    void Unsubscribe(IObserver<T> observer)
    {
        IDisposable? listening = null;
        lock (_lock)
        {
            _observers.Remove(observer);
            if (_observers.Count == 0)
            {
                listening = _listening;
                _listening = null;
            }
        }

        listening?.Dispose();
    }

    sealed class Observation(SemanticLiveQuery<T> query, IObserver<T> observer) : IDisposable
    {
        public void Dispose() => query.Unsubscribe(observer);
    }

    sealed class NoSubscription : IDisposable
    {
        internal static readonly NoSubscription Instance = new();

        public void Dispose()
        {
        }
    }
}
