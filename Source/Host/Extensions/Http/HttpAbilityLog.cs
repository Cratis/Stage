// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Http;

/// <summary>
/// Log messages of the built-in <c lang="text">http</c> extension. They name the system, the ability and the status, never a URL, header, query or body, because those can hold a secret.
/// </summary>
public static partial class HttpAbilityLog
{
    [LoggerMessage(LogLevel.Information, "The ability {System}.{Ability} answered {Status} for invocation {Invocation}")]
    internal static partial void Answered(ILogger logger, string system, string ability, int status, string invocation);

    [LoggerMessage(LogLevel.Warning, "The http request of {System}.{Ability} did not complete for invocation {Invocation}: {Reason}")]
    internal static partial void DidNotComplete(ILogger logger, string system, string ability, string invocation, string reason);

    [LoggerMessage(LogLevel.Warning, "The http ability {System}.{Ability} was not sent: {Reason}")]
    internal static partial void NotSent(ILogger logger, string system, string ability, string reason);
}
