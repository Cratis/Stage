// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_a_protected_live_list_denial : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var source = "policy Clerks\n  require authenticated\n" + a_query_filter_model.Source
            .Replace("query AllInvoices => observable InvoiceSummary[]\n", "query AllInvoices => observable InvoiceSummary[]\n        authorize Clerks\n", StringComparison.Ordinal) + "\n" + """
                  specification DenyingList
                    given caller
                    then query AllInvoices
                    then denied
            """;
        _plan = invoice_model.Plan(invoice_model.Compile(source));
    }

    [Fact] void should_admit_the_denial() => _plan.Diagnostics.WithoutAuthenticatedDefaultNotes().ShouldBeEmpty();
    [Fact] void should_execute_the_argument_free_query_through_arc() => Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("when_denying_list_is_queried.cs", StringComparison.Ordinal)).Bytes.AsSpan()).ShouldContain("_scenario.Perform(nameof(InvoiceSummary.AllInvoices), new QueryArguments {  })");
}
