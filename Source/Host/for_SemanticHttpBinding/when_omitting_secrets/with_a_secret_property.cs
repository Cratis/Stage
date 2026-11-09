// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHttpBinding.when_omitting_secrets;

public class with_a_secret_property : given.a_secret_application
{
    Task Because() => Post("register", $"{{\"username\":\"u\",\"password\":\"{SecretValue}\"}}");

    [Fact] void should_succeed() => _status.ShouldEqual(200);
    [Fact] void should_not_return_the_secret_property() => _result.GetProperty("response").TryGetProperty("password", out _).ShouldBeFalse();
    [Fact] void should_not_contain_the_secret_value_anywhere() => _body.ShouldNotContain(SecretValue);
    [Fact] void should_still_return_the_other_properties() => _result.GetProperty("response").GetProperty("username").GetString().ShouldEqual("u");
}
