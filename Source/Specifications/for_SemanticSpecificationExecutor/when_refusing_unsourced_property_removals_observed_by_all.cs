// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_refusing_unsourced_property_removals_observed_by_all : Specification
{
    const string AllObserver = """
            slice StateView AllInvoiceLookup
              event InvoiceRegistered
                description String
              readmodel AllInvoiceView
                invoiceId String key
                lastSeen String?
              projection AllInvoices => AllInvoiceView
                from InvoiceRegistered
                  lastSeen = description
                all
                  lastSeen = $eventSourceId
        """;

    SemanticSpecificationRunRecord _result = null!;

    async Task Because()
    {
        var plan = when_running_property_keyed_removal_only_projections.Compile(
            when_running_property_keyed_removal_only_projections.Source + "\n" + AllObserver);
        _result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
    }

    [Fact] void should_refuse_the_unsourced_append() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_report_the_projection_capability() => _result.Unsupported!.Capability.ShouldEqual(StageExecutionCapability.Projection);
    [Fact] void should_preserve_the_source_fidelity_reason() => _result.Unsupported!.Details.ShouldEqual("A scoped event-source key must have the read-model identifier's value type and enumeration values.");
}
