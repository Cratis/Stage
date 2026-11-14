// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication.given;

sealed class OfflineCredentialEventLoggerProvider(OfflineAuthenticationDiagnostics diagnostics) : ILoggerProvider
{
    internal const string Category = "Microsoft.Identity.Web.CredentialsProvider";
    readonly ILogger _logger = new CredentialEventLogger(diagnostics);

    public ILogger CreateLogger(string categoryName) =>
        string.Equals(categoryName, Category, StringComparison.Ordinal) ? _logger : NullLogger.Instance;

    public void Dispose()
    {
    }

    sealed class CredentialEventLogger(OfflineAuthenticationDiagnostics diagnostics) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            // Never inspect, retain or format state, exception, formatter or EventId.Name.
            if (IsEnabled(logLevel))
            {
                diagnostics.RecordCredentialEvent(eventId.Id);
            }
        }
    }
}
