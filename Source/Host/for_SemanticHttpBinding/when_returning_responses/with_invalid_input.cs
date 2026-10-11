// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHttpBinding.when_returning_responses;

public class with_invalid_input : given.a_response_application
{
    Task Because() => Post("register-identifier", "register-identifier", "{\"name\":\"\"}");

    [Fact] void should_reject_the_input() => _status.ShouldEqual(400);
    [Fact] void should_have_no_response() => HasNoResponse().ShouldBeTrue();
    [Fact] void should_append_nothing() => _facts.ShouldBeEmpty();
}
