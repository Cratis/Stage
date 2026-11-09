// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using Cratis.Arc.Identity;
using Cratis.Specifications;
using Microsoft.AspNetCore.Authentication;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxIdentity;

public class when_accepting_a_selected_user : Specification
{
    ServiceProvider _provider = null!;

    [Theory]
    [InlineData("192.0.2.1", false, false)]
    [InlineData("2001:db8::1", false, false)]
    [InlineData("127.0.0.1", false, true)]
    [InlineData("::1", false, true)]
    [InlineData("::ffff:127.0.0.1", false, true)]
    [InlineData("::ffff:192.0.2.1", false, false)]
    [InlineData("192.0.2.1", true, true)]
    [InlineData(null, false, false)]
    public async Task should_accept_the_selected_user_only_from_an_allowed_connection(string? address, bool allowRemote, bool accepted)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        SandboxIdentityOptions.Configure(services, new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                [SandboxUsersOptions.Key] = "true",
                [$"{SandboxUsersOptions.Key}:AllowRemote"] = allowRemote.ToString()
            }).Build());
        services.AddAuthentication(MicrosoftIDentityPlatformAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, MicrosoftIDentityPlatformAuthHandler>(MicrosoftIDentityPlatformAuthHandler.SchemeName, _ => { });
        _provider = services.BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = _provider };
        context.Connection.RemoteIpAddress = address is null ? null : IPAddress.Parse(address);
        context.Request.Headers["X-Forwarded-For"] = "127.0.0.1";
        context.Request.Headers["x-ms-client-principal-id"] = "selected-user";
        context.Request.Headers["x-ms-client-principal-name"] = "Selected user";
        context.Request.Headers["x-ms-client-principal"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(
            """{"identityProvider":"aad","userId":"selected-user","userDetails":"Selected user","userRoles":["Reader"]}"""));
        context.Request.Headers["x-ms-client-principal-extra"] = "selected-value";
        var pipeline = new ApplicationBuilder(_provider);
        pipeline.UseSandboxIdentityHeaders();
        pipeline.Run(_ => Task.CompletedTask);

        await pipeline.Build()(context);

        context.Request.Headers.ContainsKey("x-ms-client-principal-id").ShouldEqual(accepted);
        context.Request.Headers.ContainsKey("x-ms-client-principal").ShouldEqual(accepted);
        context.Request.Headers.ContainsKey("x-ms-client-principal-name").ShouldEqual(accepted);
        context.Request.Headers.ContainsKey("x-ms-client-principal-extra").ShouldEqual(accepted);
        context.User.Identity!.IsAuthenticated.ShouldEqual(accepted);
    }

    void Destroy() => _provider?.Dispose();
}
