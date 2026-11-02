// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_guarding_the_authentication_perimeter;

public class when_posting_from_a_different_port : given.an_enabled_perimeter
{
    Task Because() => Send(HttpMethod.Post, "/protected", new() { Cookie = true, Origin = "https://app.example.invalid:444" });
    [Fact] void should_forbid_the_request() => _response.StatusCode.ShouldEqual(HttpStatusCode.Forbidden);
}
