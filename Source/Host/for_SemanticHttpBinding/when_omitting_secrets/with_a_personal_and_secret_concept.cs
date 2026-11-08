// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHttpBinding.when_omitting_secrets;

public class with_a_personal_and_secret_concept : given.a_secret_application
{
    Task Because() => Post("protect", $"{{\"name\":\"n\",\"token\":\"{SecretValue}\"}}");

    [Fact] void should_succeed() => _status.ShouldEqual(200);
    [Fact] void should_not_contain_the_secret_value_anywhere() => _body.ShouldNotContain(SecretValue);
    [Fact] void should_omit_the_property() => _result.GetProperty("response").TryGetProperty("token", out _).ShouldBeFalse();
}
