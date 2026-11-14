// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication.given;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication;

[Collection(nameof(offline_authentication_collection))]
public class when_disabling_host_authentication(OfflineAuthenticationFixture fixture) : Specification
{
    readonly List<HttpStatusCode> _statuses = [];
    readonly List<bool> _registered = [];

    async Task Because()
    {
        using var scenario = fixture.BeginScenario();
        foreach (var enabled in new[] { null, "false" })
        {
            var settings = new Dictionary<string, string?>
            {
                ["Stage:Authentication:EntraId:Enabled"] = enabled,
                ["Stage:Authentication:PublicOrigin"] = "invalid-ignored-while-disabled",
                ["Stage:Authentication:EntraId:TenantId"] = "invalid-ignored-while-disabled"
            };
            await using var host = await OfflineHost.Start(fixture, settings);
            var schemes = host.Services.GetRequiredService<IAuthenticationSchemeProvider>();
            _registered.Add((await schemes.GetAllSchemesAsync()).Any(scheme => scheme.Name.StartsWith("GeneratedHost", StringComparison.Ordinal)));
            using var response = await host.Send("/.cratis/discovery", new HostRequest { Method = "POST" });
            _statuses.Add(response.StatusCode);
        }
    }

    [Fact] void should_leave_default_anonymous_behavior_unchanged_without_origin() => _statuses.ShouldEqual(Enumerable.Repeat(HttpStatusCode.OK, 2));
    [Fact] void should_not_register_generated_host_schemes_when_absent_or_false() => _registered.ShouldEqual(Enumerable.Repeat(false, 2));
}
