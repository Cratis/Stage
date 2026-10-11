// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHttpBinding.when_returning_responses;

public class with_a_null_scalar : given.a_response_application
{
    Task Because() => Post("return-optional", "return-optional", "{\"value\":null}");

    [Fact] void should_succeed() => _status.ShouldEqual(200);
    [Fact] void should_not_fall_back_to_the_payload() => HasNoResponse().ShouldBeTrue();
    [Fact] void should_append_nothing() => _facts.ShouldBeEmpty();
}
