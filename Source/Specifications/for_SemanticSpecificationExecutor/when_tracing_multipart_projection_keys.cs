// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Specifications.Commands;
using Cratis.Stage.Specifications.Comparison;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_tracing_multipart_projection_keys : Specification
{
    string _first = null!;
    string _sharedPart = null!;
    string _reversed = null!;
    bool _sameSemanticKey;
    bool _scalarsUnchanged;

    void Because()
    {
        var tenant = new SemanticProperty(SemanticId.Parse($"sem1:{1:x64}"), "tenant", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), true);
        var invoice = new SemanticProperty(SemanticId.Parse($"sem1:{2:x64}"), "invoice", tenant.Type, true);
        var model = new SemanticReadModel(SemanticId.Parse($"sem1:{3:x64}"), "Payment", [tenant, invoice]);
        var first = SemanticValue.Composite([new(tenant.Id, SemanticValue.Text("north|a")), new(invoice.Id, SemanticValue.Text("b"))]);
        var reversed = SemanticValue.Composite([new(invoice.Id, SemanticValue.Text("b")), new(tenant.Id, SemanticValue.Text("north|a"))]);
        var shared = SemanticValue.Composite([new(tenant.Id, SemanticValue.Text("north|a")), new(invoice.Id, SemanticValue.Text("c"))]);
        _first = SemanticRunTraceKeys.Canonical(first, model);
        _sharedPart = SemanticRunTraceKeys.Canonical(shared, model);
        _reversed = SemanticRunTraceKeys.Canonical(reversed, model with { Properties = [invoice, tenant] });
        _sameSemanticKey = SemanticExpectationComparer.AreEqual(first, reversed);
        var scalars = new[] { SemanticValue.Text("quoted\"é:|"), SemanticValue.Number(12.50m), SemanticValue.Boolean(true), SemanticValue.Null };
        _scalarsUnchanged = scalars.All(key => SemanticRunTraceKeys.Canonical(key, model) == SemanticRunContext.Canonical(key));
    }

    [Fact] void should_keep_distinct_keys_with_a_shared_part() => _first.ShouldNotEqual(_sharedPart);
    [Fact] void should_follow_existing_order_independent_semantic_equality() => _sameSemanticKey.ShouldBeTrue();
    [Fact] void should_ignore_key_and_model_declaration_order() => _first.ShouldEqual(_reversed);
    [Fact] void should_keep_scalar_trace_output_byte_for_byte() => _scalarsUnchanged.ShouldBeTrue();
}
