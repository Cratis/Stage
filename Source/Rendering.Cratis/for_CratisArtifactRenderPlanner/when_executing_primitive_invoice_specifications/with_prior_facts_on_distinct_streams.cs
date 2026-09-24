// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

using context = Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_primitive_invoice_specifications.with_prior_facts_on_distinct_streams.context;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_primitive_invoice_specifications;

public class with_prior_facts_on_distinct_streams(context fixture) : IClassFixture<context>
{
    [Fact] void should_build_debug_without_warnings() => fixture.DebugWarnings.ShouldEqual(string.Empty);
    [Fact] void should_build_release_without_warnings() => fixture.ReleaseWarnings.ShouldEqual(string.Empty);
    [Fact] void should_pass_all_generated_assertions() => fixture.Results.All(_ => _.Outcome == "Passed").ShouldBeTrue();
    [Fact] void should_run_four_assertions() => fixture.Results.Length.ShouldEqual(4);

    public class context : a_generated_invoice_application
    {
        protected override string InvoiceSource => invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource).Replace(
            "when IssueInvoice\n          description = \"First payload\"",
            "given InvoiceIssued\n          for \"prior-one\"\n          description = \"Prior first\"\n        given InvoiceIssued\n          for \"prior-two\"\n          description = \"Prior second\"\n        when IssueInvoice\n          description = \"First payload\"",
            StringComparison.Ordinal);

        Task Because() => VerifyGeneratedApplication();
    }
}
#endif
