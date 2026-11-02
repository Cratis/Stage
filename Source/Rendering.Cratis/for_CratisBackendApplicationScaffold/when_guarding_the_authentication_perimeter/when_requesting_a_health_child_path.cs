// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_guarding_the_authentication_perimeter;

public class when_requesting_a_health_child_path : given.an_enabled_perimeter
{
    Task Because() => Send(HttpMethod.Get, "/healthz/x");
    [Fact] void should_not_exempt_a_child_path() => _response.StatusCode.ShouldEqual(HttpStatusCode.Unauthorized);
}
