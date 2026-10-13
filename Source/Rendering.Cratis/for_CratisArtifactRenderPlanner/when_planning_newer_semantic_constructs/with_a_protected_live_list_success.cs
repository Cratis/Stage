// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_a_protected_live_list_success : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var source = "policy Clerks\n  require authenticated\n" + a_query_filter_model.Source
            .Replace("query FilteredInvoices => observable InvoiceSummary[]\n", "query FilteredInvoices => observable InvoiceSummary[]\n        authorize Clerks\n", StringComparison.Ordinal) + "\n" + a_query_filter_model.Specification
            .Replace("specification FilteringInvoices\n", "specification FilteringInvoices\n        given caller\n          authenticated\n", StringComparison.Ordinal);
        _plan = invoice_model.Plan(invoice_model.Compile(source));
    }

    [Fact] void should_refuse_the_unsupported_success_scenario() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly("STAGE-ESM-011");
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
