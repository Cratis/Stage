// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_primitive_invoice_specifications;

public class with_duplicate_unordered_events : a_generated_invoice_application
{
    internal static string Source => with_unordered_events.Source
        .Replace("produces InvoiceRecorded", "produces InvoiceIssued", StringComparison.Ordinal)
        .Replace("then InvoiceRecorded", "then InvoiceIssued", StringComparison.Ordinal);

    protected override string InvoiceSource => Source;

    protected override ArtifactRenderPlan CreatePlan()
    {
        var plan = base.CreatePlan();
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        return plan;
    }

    Task Because() => VerifyGeneratedApplication();

    [Fact] void should_build_debug_without_warnings() => DebugWarnings.ShouldBeEmpty();
    [Fact] void should_build_release_without_warnings() => ReleaseWarnings.ShouldBeEmpty();
    [Fact] void should_run_the_multiset_matcher_once() => Results.Count(_ => _.Name.Contains("should_append_the_expected_event_multiset", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_run_the_exact_count_check_once() => Results.Count(_ => _.Name.Contains("should_append_exactly_2_events", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_pass_every_generated_specification() => (Results.Length > 0 && Results.All(_ => _.Outcome == "Passed")).ShouldBeTrue();
}
#endif
