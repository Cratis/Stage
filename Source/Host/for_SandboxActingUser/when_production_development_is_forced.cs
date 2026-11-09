// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxActingUser;

public class when_production_development_is_forced : given.a_started_host
{
    protected override bool Production => true;

    protected override void ConfigureDevelopment(WebApplicationBuilder builder)
    {
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { [StageDevelopment.Key] = "true" });
        SandboxActingUserAccess.Configure(builder, StageHostMode.Sandbox).ShouldBeTrue();
    }

    Task Because() => SendIdentity();

    [Fact] void should_inform_the_host_that_stage_is_development() => _app.Environment.IsDevelopment().ShouldBeTrue();
    [Fact] void should_retain_the_original_production_boundary() => _app.Services.GetRequiredService<SandboxActingUserAccess>().Production.ShouldBeTrue();
    [Fact] void should_not_map_the_cookie() => _principal.ShouldBeNull();
    [Fact] void should_not_map_a_principal_id() => _principalId.ShouldBeNull();
    [Fact] void should_not_authenticate_the_cookie() => _authenticated.ShouldBeFalse();

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    async Task should_hide_the_selection_surface(string method)
    {
        using var response = await _client.SendAsync(new HttpRequestMessage(new HttpMethod(method), SandboxActingUser.Route));
        response.StatusCode.ShouldEqual(HttpStatusCode.NotFound);
    }
}
