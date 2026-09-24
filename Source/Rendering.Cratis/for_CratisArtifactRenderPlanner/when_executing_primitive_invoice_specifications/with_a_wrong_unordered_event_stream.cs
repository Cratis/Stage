// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_primitive_invoice_specifications;

public class with_a_wrong_unordered_event_stream : a_generated_application
{
    protected override ArtifactRenderPlan CreatePlan()
    {
        var plan = invoice_model.Plan(invoice_model.Compile(with_duplicate_unordered_events.Source));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        return plan;
    }

    Exception? _failure;

    async Task Because()
    {
        var original = ReadGeneratedFile("Billing/Invoicing/Issue/when_issuing_an_invoice.cs");
        var matcher = original.IndexOf("var match0 =", StringComparison.Ordinal);
        (matcher >= 0).ShouldBeTrue();
        var suffix = original[matcher..];
        suffix.Contains("invoice-one", StringComparison.Ordinal).ShouldBeTrue();
        suffix = suffix.Replace("invoice-one", "invoice-two", StringComparison.Ordinal);
        var tampered = original[..matcher] + suffix;
        tampered = tampered.Replace("when_issuing_an_invoice", "when_swapped_sources", StringComparison.Ordinal);
        AddGeneratedSpecification("Billing/Invoicing/Issue/when_swapped_sources.cs", tampered);
        await Run("swapped-debug.log", "build", "-c", "Debug", "-warnaserror");
        _failure = await Catch.Exception(() => Run("swapped-test.log", "test", "-c", "Debug", "--no-build"));
    }

    [Fact] void should_fail_in_the_generated_multiset_matcher() => _failure!.Message.ShouldContain("should_append_the_expected_event_multiset");
    [Fact] void should_report_the_unmatched_stream() => _failure!.Message.ShouldContain("Failed");
}
#endif
