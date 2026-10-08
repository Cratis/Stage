// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host;

internal static partial class SemanticSpecificationRunsLogging
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Specification runner exited {ExitCode}. stdout: {Output} stderr: {Error}")]
    internal static partial void SpecificationProcessFinished(this ILogger logger, int exitCode, string output, string error);

    [LoggerMessage(Level = LogLevel.Error, Message = "Specification runner failed")]
    internal static partial void SpecificationProcessFailed(this ILogger logger, Exception exception);
}
