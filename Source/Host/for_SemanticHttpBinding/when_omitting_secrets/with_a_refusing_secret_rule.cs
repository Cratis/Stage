// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHttpBinding.when_omitting_secrets;

public class with_a_refusing_secret_rule : given.a_secret_application
{
    Task Because() => Post("strict", $"{{\"name\":\"n\",\"password\":\"{SecretValue}\"}}");

    [Fact] void should_refuse_the_command() => _status.ShouldEqual(400);
    [Fact] void should_not_contain_the_secret_value_anywhere() => _body.ShouldNotContain(SecretValue);
    [Fact] void should_report_the_authored_message() => _body.ShouldContain("Password is too short");
}
