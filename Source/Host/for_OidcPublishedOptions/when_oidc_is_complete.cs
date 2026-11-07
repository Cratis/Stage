// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.Published.for_OidcPublishedOptions;

public class when_oidc_is_complete : for_PublishedOptions.a_published_configuration
{
    IReadOnlyList<string> _problems = null!;

    void Establish()
    {
        _values["Stage:Published:Authentication:Mode"] = "Oidc";
        _values.Remove("Stage:Published:TrustedProxies:0");
        _values.Remove("Stage:Published:TrustedProxies:1");
        _values["Cratis:Authentication:PublicOrigin"] = "https://app.example";
        _values["Cratis:Authentication:EntraId:TenantId"] = "tenant";
        _values["Cratis:Authentication:EntraId:ClientId"] = "client";
        _values["Cratis:Authentication:EntraId:ClientCredentials:0:SourceType"] = "ClientSecret";
        _values["Cratis:Authentication:EntraId:ClientCredentials:0:ClientSecret"] = "spec-only";
    }

    void Because() => _problems = Options().Problems();

    [Fact] void should_accept_oidc_without_a_trusted_proxy() => _problems.ShouldBeEmpty();
}
