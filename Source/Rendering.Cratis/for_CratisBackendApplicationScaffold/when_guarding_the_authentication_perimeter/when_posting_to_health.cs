// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_guarding_the_authentication_perimeter;

public class when_posting_to_health : given.an_enabled_perimeter
{
    Task Because() => Send(HttpMethod.Post, "/healthz", new() { Origin = given.the_emitted_perimeter.PublicOrigin });
    [Fact] void should_not_exempt_an_unsafe_health_request() => _response.StatusCode.ShouldEqual(HttpStatusCode.Unauthorized);
}
