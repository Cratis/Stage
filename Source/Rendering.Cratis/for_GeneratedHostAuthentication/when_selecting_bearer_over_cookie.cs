// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication.given;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication;

[Collection(nameof(offline_authentication_collection))]
public class when_selecting_bearer_over_cookie(OfflineAuthenticationFixture fixture) : Specification
{
    readonly List<HttpStatusCode> _statuses = [];
    readonly List<bool> _redirects = [];
    string?[] _defaults = [];

    async Task Because()
    {
        using var scenario = fixture.BeginScenario();
        await using var host = await OfflineHost.Start(fixture);
        var options = host.Services.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
        _defaults = [options.DefaultScheme, options.DefaultAuthenticateScheme, options.DefaultChallengeScheme, options.DefaultForbidScheme];
        var cookie = host.SessionCookie();
        using var baseline = await host.Send("/api/query", new HostRequest { Cookie = cookie });
        baseline.StatusCode.ShouldEqual(HttpStatusCode.OK);
        foreach (var header in new[] { "", "Basic invalid", "Bearer", "Bearer malformed" })
        {
            using var response = await host.Send("/", new HostRequest { Cookie = cookie, Authorization = header, Navigate = true });
            _statuses.Add(response.StatusCode);
            _redirects.Add(response.Headers.Location is not null);
        }
    }

    [Fact] void should_override_all_earlier_authentication_defaults() => _defaults.ShouldEqual(Enumerable.Repeat("GeneratedHost", 4));
    [Fact] void should_never_downgrade_an_authorization_header_to_the_valid_cookie() => _statuses.ShouldEqual(Enumerable.Repeat(HttpStatusCode.Unauthorized, 4));
    [Fact] void should_not_initiate_browser_login_for_a_failed_bearer() => _redirects.ShouldEqual(Enumerable.Repeat(false, 4));
}
