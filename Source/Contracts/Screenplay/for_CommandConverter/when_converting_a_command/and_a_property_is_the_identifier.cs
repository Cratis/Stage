// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Commands;
using Xunit;

namespace Cratis.Stage.Contracts.Screenplay.for_CommandConverter.when_converting_a_command;

public class and_a_property_is_the_identifier : given.a_command_syntax
{
    CommandDefinition _definition = null!;

    void Because() => _definition = CommandConverter.Convert(Command(("invoiceId", true), ("invoiceNumber", false)), _schema, SlicePath);

    [Fact] void should_carry_the_name_of_the_identifying_property() => _definition.Identifier.ShouldEqual("invoiceId");
}
