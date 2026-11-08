// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHttpBinding.when_omitting_secrets;

public class with_a_validate_only_request : given.a_secret_application
{
    Task Because() => Post("return-record", $"{{\"name\":\"n\",\"password\":\"{SecretValue}\"}}", "/validate");

    [Fact] void should_succeed() => _status.ShouldEqual(200);
    [Fact] void should_keep_the_other_fields() => _result.GetProperty("response").GetProperty("name").GetString().ShouldEqual("n");
    [Fact] void should_not_contain_the_secret_value_anywhere() => _body.ShouldNotContain(SecretValue);
}
