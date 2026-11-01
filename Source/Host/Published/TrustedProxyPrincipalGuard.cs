// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Published;

/// <summary>
/// Drops the sign-in proxy's principal headers from any request that did not come from a trusted proxy, so a caller who
/// reaches the host directly cannot present itself as someone else. Requests from the proxy pass unchanged.
/// </summary>
internal static class TrustedProxyPrincipalGuard
{
    const string PrincipalHeaderPrefix = "x-ms-client-principal";

    internal static IApplicationBuilder UseTrustedProxyPrincipalGuard(this IApplicationBuilder app, IReadOnlyList<TrustedProxyNetwork> trusted) => app.Use((context, next) =>
    {
        if (!trusted.Any(network => network.Contains(context.Connection.RemoteIpAddress)))
        {
            foreach (var name in context.Request.Headers.Keys.Where(name => name.StartsWith(PrincipalHeaderPrefix, StringComparison.OrdinalIgnoreCase)).ToArray())
            {
                context.Request.Headers.Remove(name);
            }
        }

        return next(context);
    });
}
