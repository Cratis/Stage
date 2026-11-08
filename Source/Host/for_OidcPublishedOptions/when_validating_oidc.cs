// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_OidcPublishedOptions;

[PerFactSpecificationLifecycle]
public class when_validating_oidc : for_OidcPublishedHost.given.an_oidc_configuration
{
    [Theory]
    [InlineData("https://app.example", true)]
    [InlineData("https://app.example:8443/", true)]
    [InlineData("http://localhost:9090", true)]
    [InlineData("http://127.0.0.1:9090", true)]
    [InlineData("http://[::1]:9090", true)]
    [InlineData("http://app.example", false)]
    [InlineData("https://user@app.example", false)]
    [InlineData("https://app.example/path", false)]
    [InlineData("https://app.example?query=1", false)]
    [InlineData("https://app.example#fragment", false)]
    [InlineData("not-an-origin", false)]
    [InlineData("", false)]
    void should_validate_the_public_origin(string origin, bool valid)
    {
        _values["Cratis:Authentication:PublicOrigin"] = origin;
        Options().Problems().Count.ShouldEqual(valid ? 0 : 1);
    }

    [Theory]
    [InlineData("Cratis:Authentication:PublicOrigin")]
    [InlineData("Cratis:Authentication:EntraId:TenantId")]
    [InlineData("Cratis:Authentication:EntraId:ClientId")]
    [InlineData("Cratis:Authentication:EntraId:ClientCredentials:0:SourceType")]
    void should_name_missing_configuration_without_values(string key)
    {
        _values.Remove(key);
        if (key.Contains("ClientCredentials", StringComparison.Ordinal)) _values.Remove("Cratis:Authentication:EntraId:ClientCredentials:0:ClientSecret");
        var problems = Options().Problems();
        problems.Count.ShouldEqual(1);
        problems[0].Contains("spec-only", StringComparison.Ordinal).ShouldBeFalse();
        problems[0].Contains(key.Split(":0:")[0], StringComparison.Ordinal).ShouldBeTrue();
    }

    [Fact]
    void should_reject_invalid_optional_proxy_addresses()
    {
        _values["Stage:Published:TrustedProxies:0"] = "not-an-address";
        Options().Problems().Count.ShouldEqual(1);
    }

    [Fact]
    void should_reject_unknown_modes()
    {
        _values["Stage:Published:Authentication:Mode"] = "Unknown";
        Options().Problems().Any(problem => problem.Contains("not supported", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
