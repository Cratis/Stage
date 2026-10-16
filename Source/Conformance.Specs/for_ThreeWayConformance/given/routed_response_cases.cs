// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;

internal static class routed_response_cases
{
    const string Pin = "11111111-1111-7111-8111-111111111111";

    internal static ExecutableSemanticModel Compile() => invoice_model.Compile($"""
        concept Receipt : Uuid
          generate uuidv7
        eventsource Account
          identifier Receipt
          stream Transactions
            streamId String
        module Banking
          feature Responses
        {Slice("Scalar", false, false)}
        {Slice("Named", true, false)}
        {Slice("Conditional", true, true)}
        {Slice("Request", false, false, false)}
        """);

    static string Slice(string name, bool named, bool conditional, bool generated = true) => $"""
            slice StateChange {name}
              command Record{name}
                receipt Receipt {(generated ? "generated identifier" : "identifier")}
                period String
                name String
        {(conditional ? "        enabled Bool" : string.Empty)}
                stream Account.Transactions
                  streamId = period
        {(conditional ? $"        produces when enabled == true\n          {name}Recorded\n            for receipt\n            name = name" : $"        produces {name}Recorded\n          for receipt\n          name = name")}
        {(named ? "        returns\n          receipt = receipt\n          name = name" : $"        returns {(generated ? "receipt" : "name")}")}
              event {name}Recorded
                name String
        {Scenario(name, named, conditional, generated, false, false)}
        {Scenario(name, named, conditional, generated, true, false)}
        {(conditional ? Scenario(name, named, conditional, generated, false, true) : string.Empty)}
        {(conditional ? Scenario(name, named, conditional, generated, true, true) : string.Empty)}
        """;

    static string Scenario(string name, bool named, bool conditional, bool generated, bool wrong, bool skipped) => $"""
              specification {name}{(skipped ? "Skipped" : "Response")}{(wrong ? "Wrong" : "Correct")}
                when Record{name}
        {(generated ? $"          for \"{Pin}\"" : $"          receipt = \"{Pin}\"")}
                  period = "a|b%"
                  name = "Apollo"
        {(conditional ? $"          enabled = {(skipped ? "false" : "true")}" : string.Empty)}
        {(skipped ? "        then no events" : $"        then {name}Recorded\n          for \"{Pin}\"\n          name = \"Apollo\"\n          stream Account.Transactions\n            streamId = \"a|b%\"")}
        {(named ? $"        then returns\n          receipt = \"{Pin}\"\n          name = \"{(wrong ? "Different" : "Apollo")}\"" : $"        then returns \"{ScalarExpectation(generated, wrong)}\"")}
        """;

    static string ScalarExpectation(bool generated, bool wrong) => (generated, wrong) switch
    {
        (true, true) => "22222222-2222-7222-8222-222222222222",
        (true, false) => Pin,
        (false, true) => "Different",
        _ => "Apollo"
    };
}
#endif
