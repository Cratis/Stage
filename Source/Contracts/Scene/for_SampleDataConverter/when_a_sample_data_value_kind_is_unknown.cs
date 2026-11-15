// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_SampleDataConverter;

public class when_a_sample_data_value_kind_is_unknown : Specification
{
    ApplicationSyntax _syntax;
    Exception _exception;

    void Establish()
    {
        var at = SourceLocation.Start;
        var field = new SampleDataFieldSyntax("lines", (SampleDataValueKind)int.MaxValue, "not text", at);
        _syntax = new([], [], [], [], at)
        {
            SampleDataPacks = [new("OrdersDemo", null, null, [new("Order", [new([field], at)], at)], at)]
        };
    }

    void Because() => _exception = Catch.Exception(() => SampleDataConverter.Packs(_syntax));

    [Fact] void should_refuse_the_unrecognized_kind() => _exception.ShouldBeOfExactType<UnknownSampleDataValueKind>();
    [Fact] void should_identify_the_unrecognized_kind() => _exception.Message.ShouldEqual("'2147483647' is not a known sample-data value kind");
}
