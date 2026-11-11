// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions;

/// <summary>
/// The log messages of the extension protocol.
/// </summary>
public static partial class ExtensionLog
{
    [LoggerMessage(LogLevel.Information, "The backend of {System} connected: {Name} {Version}")]
    internal static partial void Connected(ILogger logger, string system, string name, string version);

    [LoggerMessage(LogLevel.Warning, "The handshake with the backend of {System} failed")]
    internal static partial void HandshakeFailed(ILogger logger, string system, Exception exception);

    [LoggerMessage(LogLevel.Warning, "The call to the backend of {System} failed for invocation {Invocation}")]
    internal static partial void InvokeFailed(ILogger logger, string system, string invocation, Exception exception);

    [LoggerMessage(LogLevel.Warning, "The extension of {System} was refused a host service call: {Reason}")]
    internal static partial void CallRefused(ILogger logger, string system, string reason);

    [LoggerMessage(LogLevel.Information, "Stage's extension host services listen on {Address}")]
    internal static partial void Listening(ILogger logger, string address);

    [LoggerMessage(Message = "{ExtensionMessage}")]
    internal static partial void Reported(ILogger logger, LogLevel level, string extensionMessage);
}
