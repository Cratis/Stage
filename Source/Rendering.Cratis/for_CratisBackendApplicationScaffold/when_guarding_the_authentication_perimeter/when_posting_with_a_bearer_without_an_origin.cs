// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_guarding_the_authentication_perimeter;

public class when_posting_with_a_bearer_without_an_origin : given.an_enabled_perimeter
{
    Task Because() => Send(HttpMethod.Post, "/protected", new() { Authorization = "Bearer <synthetic-token>" });
    [Fact] void should_allow_the_service_caller() => _response.StatusCode.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_reach_the_protected_endpoint() => _body.ShouldEqual("protected");
}
