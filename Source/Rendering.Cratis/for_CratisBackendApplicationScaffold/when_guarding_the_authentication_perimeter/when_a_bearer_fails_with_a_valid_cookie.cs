// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_guarding_the_authentication_perimeter;

public class when_a_bearer_fails_with_a_valid_cookie : given.an_enabled_perimeter
{
    Task Because() => Send(HttpMethod.Get, "/protected", new() { Cookie = true, Authorization = "Bearer <synthetic-invalid>", Accept = "text/html" });
    [Fact] void should_not_fall_back_to_the_cookie() => _response.StatusCode.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_challenge_only_the_bearer() => _response.Headers.GetValues("Challenged-Scheme").Single().ShouldEqual(given.the_emitted_perimeter.Bearer);
    [Fact] void should_not_redirect() => _response.Headers.Location.ShouldBeNull();
}
