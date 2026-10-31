// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_guarding_the_authentication_perimeter;

public class when_navigating_with_a_query : given.an_enabled_perimeter
{
    Task Because() => Send(HttpMethod.Get, "/some/path?x=1", new() { Accept = "text/html" });
    [Fact] void should_challenge_oidc() => _response.StatusCode.ShouldEqual(HttpStatusCode.Found);
    [Fact] void should_preserve_the_exact_relative_return_url() => _response.Headers.GetValues("Return-Url").Single().ShouldEqual("/some/path?x=1");
}
