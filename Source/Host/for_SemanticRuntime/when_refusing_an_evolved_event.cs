// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime;

public class when_refusing_an_evolved_event : Specification
{
    SemanticRuntimeAdmission _admission = null!;
    SemanticExecutionPlan _plan = null!;
    SemanticEventContract _event = null!;

    void Establish()
    {
        const string source = """
            module Orders
              feature Ordering
                slice StateChange PlaceOrder
                  command PlaceOrder
                    id Uuid identifier
                    amount Decimal
                    produces OrderPlaced
                      for id
                      amount = amount
                  event OrderPlaced generation 1
                    oldAmount Decimal
                  event OrderPlaced generation 2
                    amount Decimal
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Orders"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("orders"), "orders", "Orders.play", source);
        var compilation = new SemanticModelCompiler().Compile("Orders", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        _plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        _event = _plan.Events.Values.Single();
    }

    void Because() => _admission = new(_plan);

    [Fact] void should_retain_the_modeled_generation() => _event.Revision.Value.ShouldEqual(2u);
    [Fact] void should_block_runtime_registration_and_append() => _admission.Blocking.Single().Artifact.ShouldEqual(_event.Id.ToString());
    [Fact] void should_report_the_event_contract_capability() => _admission.Blocking.Single().Capability.ShouldEqual("EventContract");
    [Fact] void should_explain_the_initial_generation_only_boundary() => _admission.Blocking.Single().Details.ShouldEqual("Only the initial event revision can be registered in Chronicle.");
}
