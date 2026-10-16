// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

public class when_refusing_unimplemented_behavior : Specification
{
    [Theory]
    [InlineData("reaction batches atomic\n", "", "STAGE-ESM-043")]
    [InlineData("command reads protected\n", "", "STAGE-ESM-044")]
    [InlineData("", "          handler\n            implementation\n              hint \"Calculate the fee\"\n", "STAGE-IMPL-002")]
    public void should_refuse_before_translating_nested_commands_or_reactions(string boundary, string handler, string code)
    {
        var source = boundary + "module Billing\n  feature Invoices\n    feature Fees\n      slice StateChange ApplyFee\n        command ApplyFee\n" + handler + "          produces FeeApplied\n        event FeeApplied";
        var exception = Catch.Exception(() => EventModelLoader.LoadFromSource(source));
        exception.ShouldBeOfExactType<InvalidEventModel>();
        exception.Message.ShouldContain(code + ":");
    }

    [Fact]
    public void should_admit_the_declarative_control() => EventModelLoader.LoadFromSource("module Billing\n  feature Invoices\n    slice StateChange ApplyFee\n      command ApplyFee\n        produces FeeApplied\n      event FeeApplied").Name.ShouldEqual("Billing");
}
