// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Commands;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

public class when_inspecting_what_the_command_reads : given.a_compiled_model_using_2x_constructs
{
    IReadOnlyList<ReadsDefinition> _reads = [];

    void Because() => _reads = _slice.Command!.Reads;

    [Fact] void should_carry_the_declared_read() => _reads.Count.ShouldEqual(1);
    [Fact] void should_name_the_read_model() => _reads[0].ReadModel.ShouldEqual("InvoiceScope");
    [Fact] void should_carry_the_property_it_is_looked_up_by() => _reads[0].By.ShouldEqual("invoiceId");
}
