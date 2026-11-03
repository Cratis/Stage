// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_guarding_the_authentication_perimeter;

public class when_handling_a_cross_origin_callback : given.an_enabled_perimeter
{
    Task Because() => Send(HttpMethod.Post, "/synthetic-callback", new() { Origin = "https://signin.invalid" });
    [Fact] void should_finish_in_the_request_handler_before_checking_origin() => _response.StatusCode.ShouldEqual(HttpStatusCode.Accepted);
}
