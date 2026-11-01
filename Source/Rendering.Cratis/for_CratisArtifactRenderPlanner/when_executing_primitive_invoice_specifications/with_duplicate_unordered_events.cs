// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_primitive_invoice_specifications;

public class with_duplicate_unordered_events(with_duplicate_unordered_events.context fixture) : IClassFixture<with_duplicate_unordered_events.context>
{
    [Fact] void should_build_debug_without_warnings() => fixture.DebugWarnings.ShouldBeEmpty();
    [Fact] void should_build_release_without_warnings() => fixture.ReleaseWarnings.ShouldBeEmpty();
    [Fact] void should_run_the_multiset_matcher_once() => fixture.Results.Count(_ => _.Matches(methodName: "should_append_the_expected_event_multiset")).ShouldEqual(1);
    [Fact] void should_run_the_exact_count_check_once() => fixture.Results.Count(_ => _.Matches(methodName: "should_append_exactly_2_events")).ShouldEqual(1);
    [Fact] void should_pass_every_generated_specification() => (fixture.Results.Length > 0 && fixture.Results.All(_ => _.Outcome == "Passed")).ShouldBeTrue();

    internal static string Source => with_unordered_events.Source
        .Replace("produces InvoiceRecorded", "produces InvoiceIssued", StringComparison.Ordinal)
        .Replace("then InvoiceRecorded", "then InvoiceIssued", StringComparison.Ordinal);

    public class context : a_generated_invoice_application
    {
        protected override string InvoiceSource => with_duplicate_unordered_events.Source;

        protected override ArtifactRenderPlan CreatePlan()
        {
            var plan = base.CreatePlan();
            Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
            return plan;
        }

        Task Because() => VerifyGeneratedApplication();
    }
}
#endif
