// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Microsoft.AspNetCore.WebUtilities;

namespace Cratis.Stage.Host.Published;

internal static class KernelConnectionGuard
{
    internal static void EnsureAllowed(string connectionString, PublishedOptions published, bool finalConnection = true)
    {
        var connection = AuthenticatedAddress(connectionString);
        if (connection.IsLoopback)
        {
            return;
        }

        if (!published.HasChronicleCredentials)
        {
            throw new PublishedModeRefused(["A non-loopback Chronicle connection requires Stage:Published:Chronicle:ClientId and Stage:Published:Chronicle:ClientSecret; development credentials are only allowed over loopback."]);
        }

        if (finalConnection && UsesDevelopmentCredentials(connection))
        {
            throw new PublishedModeRefused(["A non-loopback Chronicle connection must contain non-development credentials, including when mounted."]);
        }
    }

    static Uri AuthenticatedAddress(string connectionString)
    {
        if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var connection))
        {
            throw new PublishedModeRefused(["The published Chronicle connection must be a valid chronicle:// address."]);
        }
        if (connection.Scheme != "chronicle" || string.IsNullOrEmpty(connection.Host))
        {
            throw new PublishedModeRefused(["The published Chronicle connection must be a valid chronicle:// address."]);
        }

        var query = QueryHelpers.ParseQuery(connection.Query);
        if (query.TryGetValue("auth", out var authentication) && authentication.Any(value => string.Equals(value, "none", StringComparison.OrdinalIgnoreCase)))
        {
            throw new PublishedModeRefused(["The published Chronicle connection must not disable authentication (auth=none)."]);
        }
        return connection;
    }

    static bool UsesDevelopmentCredentials(Uri connection)
    {
        var development = new ChronicleConnectionStringBuilder(ChronicleConnectionString.Development.ToString())
            .WithDevelopmentCredentials()
            .ToConnectionString();
        return string.IsNullOrEmpty(connection.UserInfo) || Uri.UnescapeDataString(connection.UserInfo) == Uri.UnescapeDataString(new Uri(development.ToString()).UserInfo);
    }
}
