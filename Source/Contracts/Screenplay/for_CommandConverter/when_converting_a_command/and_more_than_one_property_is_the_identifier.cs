// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Screenplay.for_CommandConverter.when_converting_a_command;

public class and_more_than_one_property_is_the_identifier : given.a_command_syntax
{
    Exception _error = null!;

    void Because() => _error = Catch.Exception(() => CommandConverter.Convert(Command(("invoiceId", true), ("customerId", true)), _schema, SlicePath));

    [Fact] void should_refuse_to_choose_between_them() => _error.ShouldBeOfExactType<AmbiguousCommandIdentifier>();
    [Fact] void should_name_both_properties() => _error.Message.ShouldContain("invoiceId, customerId");
}
