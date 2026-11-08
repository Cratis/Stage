// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHttpBinding.when_omitting_secrets;

public class without_a_secret_property : given.a_secret_application
{
    Task Because() => Post("rename", "{\"name\":\"n\",\"note\":\"hello\"}");

    [Fact] void should_succeed() => _status.ShouldEqual(200);
    [Fact] void should_echo_the_payload_unchanged() => _result.GetProperty("response").GetRawText().ShouldEqual("{\"name\":\"n\",\"note\":\"hello\"}");
}
