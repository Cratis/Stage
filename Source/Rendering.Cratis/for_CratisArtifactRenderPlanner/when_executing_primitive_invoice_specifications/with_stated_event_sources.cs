// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

using context = Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_primitive_invoice_specifications.with_stated_event_sources.context;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_primitive_invoice_specifications;

public class with_stated_event_sources(context fixture) : IClassFixture<context>
{
    [Fact] void should_build_debug_without_warnings() => fixture.DebugWarnings.ShouldEqual(string.Empty);
    [Fact] void should_build_release_without_warnings() => fixture.ReleaseWarnings.ShouldEqual(string.Empty);
    [Fact] void should_execute_all_four_generated_facts() => fixture.Results.Length.ShouldEqual(4);
    [Fact] void should_pass_every_generated_fact() => fixture.Results.All(_ => _.Outcome == "Passed").ShouldBeTrue();
    [Fact] void should_verify_the_stated_first_stream() => fixture.Results.Any(_ => _.Name.EndsWith(".when_issuing_first_invoice.should_have_appended_invoice_issued", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_verify_the_stated_second_stream() => fixture.Results.Any(_ => _.Name.EndsWith(".when_issuing_second_invoice.should_have_appended_invoice_issued", StringComparison.Ordinal)).ShouldBeTrue();

    public class context : a_generated_invoice_application
    {
        // ESM v2: the first command and both expected events name the stream they act on.
        protected override string InvoiceSource => invoice_model.Source("Uuid", invoice_model.UuidSource, invoice_model.OtherUuidSource)
            .Replace("when IssueInvoice\n          description = \"First", "when IssueInvoice\n          for " + invoice_model.UuidSource + "\n          description = \"First", StringComparison.Ordinal)
            .Replace("then InvoiceIssued\n          description = \"First", "then InvoiceIssued\n          for " + invoice_model.UuidSource + "\n          description = \"First", StringComparison.Ordinal)
            .Replace("then InvoiceIssued\n          description = \"Second", "then InvoiceIssued\n          for " + invoice_model.OtherUuidSource + "\n          description = \"Second", StringComparison.Ordinal);

        Task Because() => VerifyGeneratedApplication();
    }
}
#endif
