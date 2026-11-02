// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_guarding_the_authentication_perimeter;

public class when_upgrading_from_a_foreign_origin : given.an_enabled_perimeter
{
    Exception? _error;

    async Task Because()
    {
        var client = _app.GetTestServer().CreateWebSocketClient();
        client.ConfigureRequest = request =>
        {
            request.Headers.Origin = "https://sibling.example.invalid";
            request.Headers.Cookie = "perimeter-session=synthetic-session";
        };
        _error = await Catch.Exception(async () =>
        {
            using var socket = await client.ConnectAsync(new Uri("ws://app.example.invalid/socket"), CancellationToken.None);
        });
    }

    [Fact] void should_reach_the_upgrade_route() => _webSocketPath.ShouldEqual("/socket");
    [Fact] void should_forbid_the_upgrade() => _webSocketStatus.ShouldEqual(HttpStatusCode.Forbidden);
    [Fact] void should_not_establish_a_websocket() => _error.ShouldNotBeNull();
}
