// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxActingUser;

public class when_the_selection_body_is_hostile : given.a_started_host
{
    [Theory]
    [InlineData("{")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"name\":42}")]
    [InlineData("{\"name\":{\"userId\":\"alice\",\"userRoles\":[\"admin\"]}}")]
    [InlineData("{\"name\":\"Alice\"}")]
    [InlineData("{\"name\":\"alice; role=admin\"}")]
    async Task should_reject_without_writing_a_cookie(string body)
    {
        await SendSelectionBody(body);
        _response.StatusCode.ShouldEqual(HttpStatusCode.BadRequest);
        HasCookie.ShouldBeFalse();
    }
}
