// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Stage.Host.Extensions.given;

/// <summary>
/// A logger factory that keeps what was logged with the scope values it was logged under.
/// </summary>
public sealed class RecordingLoggers : ILoggerFactory, ILoggerProvider
{
    public List<(string Category, LogLevel Level, string Message, Dictionary<string, object?> Scope)> Records { get; } = [];

    public void AddProvider(ILoggerProvider provider)
    {
    }

    public ILogger CreateLogger(string categoryName) => new Recording(this, categoryName);

    public void Dispose()
    {
    }

    sealed class Recording(RecordingLoggers owner, string category) : ILogger
    {
        static readonly AsyncLocal<Dictionary<string, object?>?> _scope = new();

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            var previous = _scope.Value;
            _scope.Value = state is IEnumerable<KeyValuePair<string, object?>> values ? values.ToDictionary(pair => pair.Key, pair => pair.Value) : [];
            return new Restore(() => _scope.Value = previous);
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            owner.Records.Add((category, logLevel, formatter(state, exception), _scope.Value ?? []));

        sealed class Restore(Action restore) : IDisposable
        {
            public void Dispose() => restore();
        }
    }
}
