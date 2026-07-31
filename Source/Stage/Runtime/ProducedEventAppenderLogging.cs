// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Stage.Runtime;

internal static partial class ProducedEventAppenderLogging
{
    [LoggerMessage(LogLevel.Warning, "Appending events for event source '{EventSourceId}' hit {Count} constraint violation(s)")]
    internal static partial void ConstraintViolations(ILogger logger, string eventSourceId, int count);

    [LoggerMessage(LogLevel.Error, "Failed to append the events produced by command '{Command}'")]
    internal static partial void AppendFailed(ILogger logger, string command, Exception exception);
}
