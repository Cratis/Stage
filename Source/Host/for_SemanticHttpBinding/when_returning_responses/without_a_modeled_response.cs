// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHttpBinding.when_returning_responses;

public class without_a_modeled_response : given.a_response_application
{
    Task Because() => Post("echo-project", "echo-project", "{\"projectId\":\"11111111-1111-1111-1111-111111111111\",\"name\":\"Apollo\"}");

    [Fact] void should_succeed() => _status.ShouldEqual(200);
    [Fact] void should_preserve_the_payload_response() => _result.GetProperty("response").GetProperty("name").GetString().ShouldEqual("Apollo");
    [Fact] void should_append_the_fact() => _facts.Count.ShouldEqual(1);
}
