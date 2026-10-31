// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_guarding_the_authentication_perimeter;

public class when_a_health_check_throws : given.an_enabled_perimeter
{
    void Establish() => _unhealthy = true;
    Task Because() => Send(HttpMethod.Get, "/healthz");
    [Fact] void should_report_unavailable() => _response.StatusCode.ShouldEqual(HttpStatusCode.ServiceUnavailable);
    [Fact] void should_return_only_the_aggregate_status() => _body.ShouldEqual("Unhealthy");
    [Fact] void should_not_disclose_exception_details() => _body.ShouldNotContain(FailureSentinel);
}
