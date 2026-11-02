// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_guarding_the_authentication_perimeter;

public class when_only_public_origin_is_configured : given.configured_sign_in
{
    void Establish() => _values = new() { [given.the_emitted_perimeter.PublicOriginKey] = given.the_emitted_perimeter.PublicOrigin };
    void Because() => Read();
    [Fact] void should_fail_closed_naming_only_the_key() => NamesOnly(given.the_emitted_perimeter.EntraIdKey).ShouldBeTrue();
}
