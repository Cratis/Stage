// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Extensions.Protocol;
using RequestContext = Cratis.Stage.Extensions.Protocol.RequestContext;

namespace Cratis.Stage.Extensions;

/// <summary>
/// One attempt at an ability, as the handler receives it.
/// </summary>
public sealed class AbilityContext
{
    internal AbilityContext(RequestContext request, string input, HostServices host, CancellationToken cancellationToken)
    {
        Request = request;
        Input = input;
        Host = host;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets the tenant the work runs for.
    /// </summary>
    public string Tenant => Request.Tenant;

    /// <summary>
    /// Gets the system whose ability is performed.
    /// </summary>
    public string System => Request.System;

    /// <summary>
    /// Gets the invocation identity.
    /// </summary>
    public string InvocationId => Request.InvocationId;

    /// <summary>
    /// Gets the key the effect is deduplicated on. It is the same for every attempt of the invocation, so the same call must have the same effect.
    /// </summary>
    public string IdempotencyKey => Request.IdempotencyKey;

    /// <summary>
    /// Gets the attempt number, starting at 1.
    /// </summary>
    public int Attempt => Request.Attempt;

    /// <summary>
    /// Gets when the attempt runs out of time, if a deadline was imposed.
    /// </summary>
    public DateTimeOffset? Deadline => Request.Deadline?.ToDateTimeOffset();

    /// <summary>
    /// Gets what caused the invocation.
    /// </summary>
    public string Causation => Request.Causation;

    /// <summary>
    /// Gets the W3C trace context, so the extension's own telemetry joins the invocation's trace.
    /// </summary>
    public string Traceparent => Request.Traceparent;

    /// <summary>
    /// Gets the ability's input, as JSON.
    /// </summary>
    public string Input { get; }

    /// <summary>
    /// Gets the host services, tied to this invocation.
    /// </summary>
    public HostServices Host { get; }

    /// <summary>
    /// Gets a token that is cancelled when the attempt is abandoned.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    internal RequestContext Request { get; }
}
