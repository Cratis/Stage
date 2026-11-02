// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_guarding_the_authentication_perimeter;

public class when_public_origin_uses_non_loopback_http : given.configured_sign_in
{
    void Establish() => _values[given.the_emitted_perimeter.PublicOriginKey] = "http://app.example.invalid";
    void Because() => Read();
    [Fact] void should_reject_it_without_disclosing_values() => NamesOnly(given.the_emitted_perimeter.PublicOriginKey).ShouldBeTrue();
}
