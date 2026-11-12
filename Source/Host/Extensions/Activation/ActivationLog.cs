// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Activation;

/// <summary>
/// The log messages of backend activation.
/// </summary>
public static partial class ActivationLog
{
    [LoggerMessage(LogLevel.Information, "Activating the {Tier} backend of {System} ({Reason})")]
    internal static partial void Activating(ILogger logger, string system, ExecutionTier tier, ActivationReason reason);

    [LoggerMessage(LogLevel.Information, "The backend of {System} is active at {Address}")]
    internal static partial void Activated(ILogger logger, string system, Uri address);

    [LoggerMessage(LogLevel.Warning, "The backend of {System} could not be started: {Reason}")]
    internal static partial void StartFailed(ILogger logger, string system, string reason);

    [LoggerMessage(LogLevel.Information, "Stopping the backend of {System} ({Reason})")]
    internal static partial void Stopping(ILogger logger, string system, string reason);

    [LoggerMessage(LogLevel.Warning, "The backend of {System} is gone: {Reason}")]
    internal static partial void Gone(ILogger logger, string system, string reason);

    [LoggerMessage(LogLevel.Warning, "Stopping the backend of {System} failed")]
    internal static partial void StopFailed(ILogger logger, string system, Exception exception);

    [LoggerMessage(LogLevel.Debug, "{System}: {Line}")]
    internal static partial void BackendOutput(ILogger logger, string system, string line);

    [LoggerMessage(LogLevel.Warning, "Activating the backend of {System} failed")]
    internal static partial void ActivationFailed(ILogger logger, string system, Exception exception);

    [LoggerMessage(LogLevel.Error, "The activation sweep failed; the next one retries")]
    internal static partial void SweepFailed(ILogger logger, Exception exception);
}
