// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

using context = Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_primitive_invoice_specifications.with_multiple_produced_events.context;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_primitive_invoice_specifications;

public class with_multiple_produced_events(context fixture) : IClassFixture<context>
{
    [Fact] void should_build_debug_without_warnings() => fixture.DebugWarnings.ShouldEqual(string.Empty);
    [Fact] void should_build_release_without_warnings() => fixture.ReleaseWarnings.ShouldEqual(string.Empty);
    [Fact] void should_run_eight_assertions() => fixture.Results.Length.ShouldEqual(8);
    [Fact] void should_pass_every_assertion() => fixture.Results.All(_ => _.Outcome == "Passed").ShouldBeTrue();

    public class context : a_generated_invoice_application
    {
        protected override string InvoiceSource => invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource)
            .Replace("      event InvoiceIssued\n", "        produces InvoiceAudited\n          for streamReference\n          description = description\n      event InvoiceAudited\n        description String\n      event InvoiceIssued\n", StringComparison.Ordinal)
            .Replace("        then InvoiceIssued\n          description = \"First payload\"", "        then InvoiceIssued\n          description = \"First payload\"\n        then InvoiceAudited\n          description = \"First payload\"", StringComparison.Ordinal)
            .Replace("        then InvoiceIssued\n          description = \"Second payload\"", "        then InvoiceIssued\n          description = \"Second payload\"\n        then InvoiceAudited\n          description = \"Second payload\"", StringComparison.Ordinal);

        Task Because() => VerifyGeneratedApplication();
    }
}
#endif
