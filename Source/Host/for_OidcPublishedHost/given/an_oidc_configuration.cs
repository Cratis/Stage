// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Published;

namespace Cratis.Stage.Host.for_OidcPublishedHost.given;

public class an_oidc_configuration : Specification
{
    protected Dictionary<string, string?> _values = new()
    {
        ["Stage:Published:Storage:ConnectionString"] = "mongodb://storage:27017",
        ["Stage:Published:Authentication:Mode"] = "Oidc",
        ["AllowedHosts"] = "app.example",
        ["Cratis:Authentication:PublicOrigin"] = "https://app.example",
        ["Cratis:Authentication:EntraId:TenantId"] = "11111111-1111-1111-1111-111111111111",
        ["Cratis:Authentication:EntraId:ClientId"] = "22222222-2222-2222-2222-222222222222",
        ["Cratis:Authentication:EntraId:ClientCredentials:0:SourceType"] = "ClientSecret",
        ["Cratis:Authentication:EntraId:ClientCredentials:0:ClientSecret"] = "spec-only"
    };

    private protected PublishedOptions Options() => PublishedOptions.From(new ConfigurationBuilder().AddInMemoryCollection(_values).Build());
}
