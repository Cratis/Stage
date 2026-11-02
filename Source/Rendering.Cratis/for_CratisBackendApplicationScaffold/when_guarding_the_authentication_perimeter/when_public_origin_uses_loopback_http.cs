// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_guarding_the_authentication_perimeter;

public class when_public_origin_uses_loopback_http : given.configured_sign_in
{
    void Establish() => _values[given.the_emitted_perimeter.PublicOriginKey] = "http://127.0.0.1:8080";
    void Because() => Read();
    [Fact] void should_accept_the_development_origin() => _error.ShouldBeNull();
    [Fact] void should_preserve_the_origin() => Origin.ShouldEqual(new Uri("http://127.0.0.1:8080"));
}
