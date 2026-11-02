// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Stage.Lifecycles;

static partial class LifecycleLog
{
    [LoggerMessage(LogLevel.Error, "A state machine or workflow could not act on committed fact {Index}")]
    internal static partial void FactFailed(ILogger logger, Exception exception, int index);

    [LoggerMessage(LogLevel.Warning, "Workflow {Workflow} is not run: {Code} {Message}")]
    internal static partial void WorkflowRefused(ILogger logger, string workflow, string code, string message);
}
