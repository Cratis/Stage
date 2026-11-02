// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_guarding_the_authentication_perimeter;

public class when_navigating_to_the_root : given.an_enabled_perimeter
{
    Task Because() => Send(HttpMethod.Get, "/", new() { Mode = "navigate" });
    [Fact] void should_challenge_oidc() => _response.StatusCode.ShouldEqual(HttpStatusCode.Found);
    [Fact] void should_use_the_root_return_url() => _response.Headers.GetValues("Return-Url").Single().ShouldEqual("/");
    [Fact] void should_use_the_oidc_scheme() => _response.Headers.GetValues("Challenged-Scheme").Single().ShouldEqual(given.the_emitted_perimeter.OpenIdConnect);
}
