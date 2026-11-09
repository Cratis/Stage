// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Api.for_StageCommandHandler.when_handling_a_command;

public class and_the_schema_has_opaque_and_undeclared_positions : given.a_command_handler
{
    const string Schema = """
        {"type":"object","properties":{
          "invoiceId":{"type":"string"},
          "credentials":{"type":"object"},
          "address":{"type":"object","properties":{"city":{"type":"string"}}},
          "choice":{"anyOf":[{"type":"object"},{"type":"string"}]},
          "empty":{}
        }}
        """;

    JsonElement _response;

    async Task Because() => _response = JsonSerializer.SerializeToElement(await HandlerWith(Schema).Handle(ContextFor("""
        {"invoiceId":"33333333-3333-3333-3333-333333333333","credentials":{"password":"opaque-test-sentinel"},
         "address":{"city":"Oslo","extra":"opaque-test-sentinel"},"extra":"opaque-test-sentinel",
         "choice":{"password":"opaque-test-sentinel"},"empty":"opaque-test-sentinel"}
        """)));

    [Fact] void should_not_echo_any_opaque_or_undeclared_value() => _response.GetRawText().ShouldNotContain("opaque-test-sentinel");
    [Fact] void should_echo_only_declared_known_values() => _response.GetRawText().ShouldEqual("""
        {"invoiceId":"33333333-3333-3333-3333-333333333333","address":{"city":"Oslo"}}
        """);
}
