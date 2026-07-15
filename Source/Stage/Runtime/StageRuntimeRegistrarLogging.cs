// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Stage.Runtime;

internal static partial class StageRuntimeRegistrarLogging
{
    [LoggerMessage(LogLevel.Information, "Registered {ReadModelCount} read model(s) and their projections for event store '{EventStore}'")]
    internal static partial void Registered(ILogger logger, int readModelCount, string eventStore);

    [LoggerMessage(LogLevel.Information, "Event model '{Model}' has no read models with projections to register")]
    internal static partial void NoReadModels(ILogger logger, string model);

    [LoggerMessage(LogLevel.Error, "Failed to register the event model's read models and projections with Chronicle")]
    internal static partial void RegistrationFailed(ILogger logger, Exception exception);
}
