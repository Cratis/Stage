// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// Log messages of ability invocations.
/// </summary>
internal static partial class InvocationLog
{
    [LoggerMessage(LogLevel.Warning, "Attempt {Attempt} of invocation {Identity} of {Capability} was not made: an earlier attempt may have reached the system and the effect happens at most once, so the invocation stopped for reconciliation")]
    internal static partial void NotRepeated(ILogger logger, string identity, string capability, int attempt);

    [LoggerMessage(LogLevel.Information, "Invocation {Identity} of {Capability} started run {Run}")]
    internal static partial void Started(ILogger logger, string identity, string capability, string run);

    [LoggerMessage(LogLevel.Warning, "Invocation {Identity} was refused: {Reason}")]
    internal static partial void Refused(ILogger logger, string identity, string reason);

    [LoggerMessage(LogLevel.Information, "Invocation {Identity} was reconciled by {Actor} with the outcome {Outcome}")]
    internal static partial void Reconciled(ILogger logger, string identity, string actor, string outcome);

    [LoggerMessage(LogLevel.Information, "Invocation {Identity} was answered by {Actor} with {Outlet}")]
    internal static partial void Answered(ILogger logger, string identity, string actor, string outlet);
}
