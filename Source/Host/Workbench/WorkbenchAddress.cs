// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Microsoft.Extensions.Options;

namespace Cratis.Stage.Host.Workbench;

/// <summary>
/// Resolves the address the Chronicle kernel serves its Workbench on.
/// </summary>
public static class WorkbenchAddress
{
    /// <summary>
    /// Resolves the Workbench address from the configured Chronicle connection.
    /// </summary>
    /// <param name="services">The application services holding the configured <see cref="ChronicleOptions"/>.</param>
    /// <returns>The address of the kernel's Workbench.</returns>
    /// <remarks>
    /// Derived from the connection string rather than configured separately, because they are always the same
    /// place: the kernel multiplexes gRPC, the API and the Workbench onto one TLS port, so the address the
    /// client already connects to is the address the Workbench is served from.
    /// </remarks>
    public static Uri For(IServiceProvider services)
    {
        var address = services.GetRequiredService<IOptions<ChronicleOptions>>().Value.ConnectionString.ServerAddress;

        return new Uri($"https://{address.Host}:{address.Port}");
    }
}
