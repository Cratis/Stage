// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHttpBinding.when_returning_responses;

public class with_a_generated_identifier : given.a_response_application
{
    Task Because() => Post("register-identifier", "register-identifier", "{\"name\":\"Apollo\"}");

    [Fact] void should_succeed() => _status.ShouldEqual(200);
    [Fact] void should_return_the_committed_event_source() => _result.GetProperty("response").GetString().ShouldEqual(AppendedId());
    [Fact] void should_return_a_nonempty_uuid() => Guid.Parse(_result.GetProperty("response").GetString()!).ShouldNotEqual(Guid.Empty);
}
