// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxActingUser;

public class when_cookie_mapping_is_not_available : Specification
{
    [Theory]
    [InlineData(false, false, true, true, true)]
    [InlineData(true, true, true, true, true)]
    [InlineData(true, false, false, true, true)]
    [InlineData(true, false, true, false, true)]
    [InlineData(true, false, true, true, false)]
    void should_ignore_the_cookie(bool sandbox, bool production, bool development, bool usersEnabled, bool local)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new SandboxActingUserAccess(sandbox ? StageHostMode.Sandbox : StageHostMode.Published, production, development));
        services.AddSingleton(new SandboxUsersOptions(usersEnabled, false));
        services.AddSingleton(new SandboxUsers([new StageUser("alice", [], new() { UserId = "alice" })]));
        using var provider = services.BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Host = new("localhost");
        context.Request.Method = "GET";
        context.Connection.RemoteIpAddress = IPAddress.Parse(local ? "127.0.0.1" : "192.0.2.10");
        context.Request.Headers.Cookie = "stage-acting-user=alice";
        SandboxActingUser.Apply(context);
        context.Request.Headers.ContainsKey("x-ms-client-principal").ShouldBeFalse();
        context.Response.Headers.ContainsKey("Set-Cookie").ShouldBeFalse();
    }
}
