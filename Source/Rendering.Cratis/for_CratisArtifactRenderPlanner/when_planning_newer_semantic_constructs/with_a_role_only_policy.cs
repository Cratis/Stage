// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_a_role_only_policy : given.an_invoice_model
{
    void Because()
    {
        var source = Invoices.Replace("        produces InvoiceIssued\n", "        authorize Staff\n        produces InvoiceIssued\n", StringComparison.Ordinal);
        Plan("policy Staff\n  require role \"Staff\"\n" + source[..source.IndexOf("      specification", StringComparison.Ordinal)]);
    }

    [Fact] void should_reject_a_policy_that_arc_would_overrestrict() => ErrorCodes.ShouldContain("STAGE-ESM-015");
    [Fact] void should_not_emit_permissive_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
