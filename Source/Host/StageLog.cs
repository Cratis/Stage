// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host;

/// <summary>
/// Structured log messages for the engine host.
/// </summary>
internal static partial class StageLog
{
    [LoggerMessage(LogLevel.Information, "Stage running event model '{ModelName}' as event store '{EventStore}'")]
    internal static partial void Running(ILogger logger, string modelName, string eventStore);
}
