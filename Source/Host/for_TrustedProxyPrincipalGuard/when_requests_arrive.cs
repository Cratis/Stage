// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.Published.for_TrustedProxyPrincipalGuard;

public class when_requests_arrive
{
    static TrustedProxyNetwork Network(string value) => TrustedProxyNetwork.TryParse(value, out var network) ? network : throw new ArgumentException(value);

    static async Task<DefaultHttpContext> Send(string remote)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(remote);
        context.Request.Headers["x-ms-client-principal"] = "principal";
        context.Request.Headers["x-ms-client-principal-id"] = "id";
        context.Request.Headers["x-ms-client-principal-name"] = "name";
        var pipeline = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());
        pipeline.UseTrustedProxyPrincipalGuard([.. new[] { "127.0.0.1", "::1", "10.1.0.0/16" }.Select(Network)]);
        pipeline.Run(_ => Task.CompletedTask);
        await pipeline.Build()(context);

        return context;
    }

    [Fact] async Task should_keep_the_principal_from_the_proxy() => (await Send("127.0.0.1")).Request.Headers.ContainsKey("x-ms-client-principal").ShouldBeTrue();
    [Fact] async Task should_keep_the_principal_from_a_trusted_network() => (await Send("10.1.4.5")).Request.Headers.ContainsKey("x-ms-client-principal-id").ShouldBeTrue();
    [Fact] async Task should_keep_the_principal_from_a_mapped_loopback() => (await Send("::ffff:127.0.0.1")).Request.Headers.ContainsKey("x-ms-client-principal-name").ShouldBeTrue();
    [Fact] async Task should_drop_the_principal_from_anyone_else() => (await Send("203.0.113.9")).Request.Headers.ContainsKey("x-ms-client-principal").ShouldBeFalse();
    [Fact] async Task should_drop_the_identity_from_anyone_else() => (await Send("203.0.113.9")).Request.Headers.ContainsKey("x-ms-client-principal-id").ShouldBeFalse();
}
