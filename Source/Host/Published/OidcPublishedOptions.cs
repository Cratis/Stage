// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Published;

internal sealed class OidcPublishedOptions(IConfiguration configuration)
{
    internal string? PublicOrigin => configuration["Cratis:Authentication:PublicOrigin"];
    internal IConfigurationSection EntraId => configuration.GetSection("Cratis:Authentication:EntraId");

    internal IEnumerable<string> Problems()
    {
        if (string.IsNullOrWhiteSpace(PublicOrigin))
        {
            yield return Missing("Cratis:Authentication:PublicOrigin");
        }
        else if (!Uri.TryCreate(PublicOrigin, UriKind.Absolute, out var origin) ||
            (origin.Scheme != Uri.UriSchemeHttps && !(origin.Scheme == Uri.UriSchemeHttp && origin.IsLoopback)) ||
            !string.IsNullOrEmpty(origin.UserInfo) || origin.AbsolutePath != "/" ||
            !string.IsNullOrEmpty(origin.Query) || !string.IsNullOrEmpty(origin.Fragment))
        {
            yield return "The configuration key 'Cratis:Authentication:PublicOrigin' must be an https origin with only a scheme, host and optional port; http is allowed for loopback hosts only.";
        }
        if (string.IsNullOrWhiteSpace(EntraId["TenantId"]))
        {
            yield return Missing("Cratis:Authentication:EntraId:TenantId");
        }
        if (string.IsNullOrWhiteSpace(EntraId["ClientId"]))
        {
            yield return Missing("Cratis:Authentication:EntraId:ClientId");
        }
        if (!EntraId.GetSection("ClientCredentials").GetChildren().Any())
        {
            yield return Missing("Cratis:Authentication:EntraId:ClientCredentials");
        }
    }

    static string Missing(string key) => $"The configuration key '{key}' is required when sign-in with Microsoft Entra ID is configured.";
}
