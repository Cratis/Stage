// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_guarding_the_authentication_perimeter;

public class when_public_origin_is_missing : given.configured_sign_in
{
    void Establish() => _values.Remove(given.the_emitted_perimeter.PublicOriginKey);
    void Because() => Read();
    [Fact] void should_fail_closed_naming_only_the_key() => NamesOnly(given.the_emitted_perimeter.PublicOriginKey).ShouldBeTrue();
    [Fact] void should_not_produce_settings() => _settings.ShouldBeNull();
}
