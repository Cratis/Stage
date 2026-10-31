// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_CommandResponseContracts.when_building_enumeration_contracts;

public class and_the_live_transport_uses_strings : given.an_enumeration_response
{
    JsonObject _contracts = null!;

    void Because() => _contracts = new CommandResponseContracts(Application, numericEnumerations: false).From([], name => name, property => property.Name);

    [Fact] void should_describe_the_response_as_a_string() => _contracts["commands"]!["Register"]!["type"]!["kind"]!.GetValue<string>().ShouldEqual("string");
    [Fact] void should_describe_the_destination_command_input_as_a_string() => _contracts["commandInputs"]!["UpdateStatus"]!["status"]!["kind"]!.GetValue<string>().ShouldEqual("string");
}
