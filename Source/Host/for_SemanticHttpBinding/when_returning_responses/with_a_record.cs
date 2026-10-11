// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHttpBinding.when_returning_responses;

public class with_a_record : given.a_response_application
{
    Task Because() => Post("register-record", "register-record", "{\"name\":\"Apollo\"}");

    [Fact] void should_succeed() => _status.ShouldEqual(200);
    [Fact] void should_return_the_committed_event_source() => _result.GetProperty("response").GetProperty("projectId").GetString().ShouldEqual(AppendedId());
    [Fact] void should_return_the_authored_field() => _result.GetProperty("response").GetProperty("name").GetString().ShouldEqual("Apollo");
}
