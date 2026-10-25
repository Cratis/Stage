// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Xunit;

namespace Cratis.Stage.Runtime.for_StageChronicleDefinitions;

// Chronicle's $add takes one event property, so a definition that adds a product cannot be handed over: adding the first operand alone would
// store a wrong total without any error.
public class when_bridging_a_product_mapping : Specification
{
    Exception? _error;
    EventModel _model = null!;

    void Establish() => _model = EventModelLoader.LoadFromSource("""
        module Catalog
          feature Items
            slice StateView Summary
              projection Summary => SummaryModel
                from LineAdded
                  add total by unitPrice * quantity
        """);

    void Because() => _error = Catch.Exception(() => StageChronicleDefinitions.Build(_model, "event-log"));

    [Fact] void should_refuse_instead_of_adding_the_first_operand_alone() => _error.ShouldBeOfExactType<UnsupportedProjectionRuntimeExpression>();
}
#endif
