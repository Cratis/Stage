// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_guarding_the_authentication_perimeter;

public class when_requesting_via_xhr : given.an_enabled_perimeter
{
    Task Because() => Send(HttpMethod.Get, "/protected", new() { Mode = "cors", Accept = "text/html" });
    [Fact] void should_return_unauthorized() => _response.StatusCode.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_not_redirect() => _response.Headers.Location.ShouldBeNull();
}
