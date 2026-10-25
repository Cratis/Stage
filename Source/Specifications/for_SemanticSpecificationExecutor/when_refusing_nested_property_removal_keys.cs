// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_refusing_nested_property_removal_keys : Specification
{
    SemanticSpecificationRunRecord _result = null!;

    async Task Because()
    {
        var source = "type RemovalDetails\n  value String\n" + when_running_property_keyed_removal_only_projections.Source
            .Replace("        invoiceId String\n", "        invoiceId RemovalDetails\n", StringComparison.Ordinal)
            .Replace("remove with InvoiceRemoved key invoiceId", "remove with InvoiceRemoved key invoiceId.value", StringComparison.Ordinal)
            .Replace("        when append InvoiceRemoved\n          invoiceId = \"first\"", "        when append InvoiceRemoved\n          invoiceId = {\"value\":\"first\"}", StringComparison.Ordinal);
        var plan = when_running_property_keyed_removal_only_projections.Compile(source);
        _result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
    }

    [Fact] void should_refuse_the_valid_but_unsupported_key_path() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_report_the_projection_capability() => _result.Unsupported!.Capability.ShouldEqual(StageExecutionCapability.Projection);
    [Fact] void should_report_the_removal_key_limit() => _result.Unsupported!.Details.ShouldEqual("A per-run removal key must be the event source or one required scalar event property with the identifier's value type.");
}
