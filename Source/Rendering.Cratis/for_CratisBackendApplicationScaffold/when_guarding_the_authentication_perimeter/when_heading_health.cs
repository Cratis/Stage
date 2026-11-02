// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_guarding_the_authentication_perimeter;

public class when_heading_health : given.an_enabled_perimeter
{
    Task Because() => Send(HttpMethod.Head, "/healthz");
    [Fact] void should_exempt_the_head_probe() => _response.StatusCode.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_return_only_the_aggregate_status_in_testserver() => _body.ShouldEqual("Healthy");
}
