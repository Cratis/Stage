// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Stage.Semantics;

internal static partial class SemanticCommandOutcomeLog
{
    [LoggerMessage(LogLevel.Information, "Refused command {Artifact}: the instance does not exist ({Details})")]
    internal static partial void MissingInstance(ILogger logger, string artifact, string details);
}
