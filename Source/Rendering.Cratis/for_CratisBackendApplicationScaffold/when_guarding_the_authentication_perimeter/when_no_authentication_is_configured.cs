// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_guarding_the_authentication_perimeter;

public class when_no_authentication_is_configured : given.configured_sign_in
{
    void Establish() => _values.Clear();
    void Because() => Read();
    [Fact] void should_leave_sign_in_disabled() => _settings.ShouldBeNull();
    [Fact] void should_not_throw() => _error.ShouldBeNull();
}
