// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_guarding_the_authentication_perimeter;

public class when_requesting_an_anonymous_endpoint : given.an_enabled_perimeter
{
    Task Because() => Send(HttpMethod.Get, "/anonymous");
    [Fact] void should_ignore_allow_anonymous_metadata() => _response.StatusCode.ShouldEqual(HttpStatusCode.Unauthorized);
}
