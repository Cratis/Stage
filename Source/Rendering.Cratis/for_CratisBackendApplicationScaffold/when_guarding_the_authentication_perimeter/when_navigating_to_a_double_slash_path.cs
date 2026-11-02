// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_guarding_the_authentication_perimeter;

public class when_navigating_to_a_double_slash_path : given.an_enabled_perimeter
{
    Task Because() => Send(HttpMethod.Get, "https://app.example.invalid//some/path?x=1", new() { Mode = "navigate" });
    [Fact] void should_challenge_oidc() => _response.StatusCode.ShouldEqual(HttpStatusCode.Found);
    [Fact] void should_fall_back_to_the_root() => _response.Headers.GetValues("Return-Url").Single().ShouldEqual("/");
}
