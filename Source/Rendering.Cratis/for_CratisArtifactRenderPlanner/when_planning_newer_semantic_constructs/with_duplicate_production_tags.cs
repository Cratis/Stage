// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

/// <summary>
/// Chronicle deduplicates append tags; Screenplay keeps both occurrences, so duplicates fail admission.
/// </summary>
public class with_duplicate_production_tags : given.an_invoice_model
{
    void Because() => Plan(Invoices
        .Replace("          for streamReference\n", "          for streamReference\n          tag \"billing\"\n", StringComparison.Ordinal)
        .Replace("      event InvoiceIssued\n", "      event InvoiceIssued\n        tag \"billing\"\n", StringComparison.Ordinal));

    [Fact] void should_reject_the_duplicate_tag() => ErrorCodes.ShouldContain("STAGE-ESM-006");
    [Fact] void should_not_generate_degraded_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
