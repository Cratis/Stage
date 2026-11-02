// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_CommandResponseContracts.when_building_enumeration_contracts;

public class and_the_generated_transport_uses_numbers : given.an_enumeration_response
{
    JsonObject _contracts = null!;

    void Because() => _contracts = new CommandResponseContracts(Application, numericEnumerations: true).From([], name => name, property => property.Name);

    [Fact] void should_describe_the_response_as_a_number() => _contracts["commands"]["Register"]["type"]["kind"].GetValue<string>().ShouldEqual("number");
    [Fact] void should_describe_the_destination_command_input_as_a_number() => _contracts["commandInputs"]["UpdateStatus"]["status"]["kind"].GetValue<string>().ShouldEqual("number");
}
