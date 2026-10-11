// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHttpBinding.when_returning_responses;

public class without_authorization : given.a_response_application
{
    Task Because() => Post("register-restricted", "register-restricted", "{\"name\":\"Apollo\"}");

    [Fact] void should_deny_the_command() => _status.ShouldEqual(403);
    [Fact] void should_have_no_response() => HasNoResponse().ShouldBeTrue();
    [Fact] void should_append_nothing() => _facts.ShouldBeEmpty();
}
