// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Projections;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

public class when_inspecting_the_composite_key : given.a_compiled_invoicing_model
{
    ProjectionDefinition _projection = null!;

    void Because() =>
        _projection = _model.Collections[0].Modules[0].Features[0].Slices
            .Single(slice => slice.Name == "InvoiceLineReport").ReadModel!.Projection!;

    [Fact] void should_emit_only_property_expression_pairs() => _projection.From["InvoiceLineItemAdded"].Key.ShouldEqual("$composite(invoiceId=invoiceId, lineNumber=lineNumber)");
}
