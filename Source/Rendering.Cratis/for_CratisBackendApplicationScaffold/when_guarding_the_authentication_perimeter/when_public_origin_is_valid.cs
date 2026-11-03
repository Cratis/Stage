// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_guarding_the_authentication_perimeter;

public class when_public_origin_is_valid : given.configured_sign_in
{
    void Because() => Read();
    [Fact] void should_accept_the_configuration() => _error.ShouldBeNull();
    [Fact] void should_preserve_the_origin() => Origin.ShouldEqual(new Uri(given.the_emitted_perimeter.PublicOrigin));
}
