// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_guarding_the_authentication_perimeter;

public class when_getting_without_an_origin : given.an_enabled_perimeter
{
    Task Because() => Send(HttpMethod.Get, "/protected", new() { Cookie = true });
    [Fact] void should_allow_the_safe_request() => _response.StatusCode.ShouldEqual(HttpStatusCode.OK);
}
