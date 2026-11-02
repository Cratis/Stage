// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_guarding_the_authentication_perimeter;

public class when_getting_health : given.an_enabled_perimeter
{
    Task Because() => Send(HttpMethod.Get, "/healthz");
    [Fact] void should_exempt_the_health_probe() => _response.StatusCode.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_return_only_the_aggregate_status() => _body.ShouldEqual("Healthy");
}
