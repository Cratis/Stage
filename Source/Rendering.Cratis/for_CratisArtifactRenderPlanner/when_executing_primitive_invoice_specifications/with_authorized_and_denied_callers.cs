// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

using context = Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_primitive_invoice_specifications.with_authorized_and_denied_callers.context;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_primitive_invoice_specifications;

public class with_authorized_and_denied_callers(context fixture) : IClassFixture<context>
{
    [Fact] void should_build_debug_without_warnings() => fixture.DebugWarnings.ShouldEqual(string.Empty);
    [Fact] void should_build_release_without_warnings() => fixture.ReleaseWarnings.ShouldEqual(string.Empty);
    [Fact] void should_pass_all_generated_assertions() => fixture.Results.All(_ => _.Outcome == "Passed").ShouldBeTrue();
    [Fact] void should_assert_the_denial() => fixture.Results.Any(_ => _.Name.EndsWith("should_be_denied", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_assert_no_event_was_appended() => fixture.Results.Any(_ => _.Name.EndsWith("should_not_append_events", StringComparison.Ordinal)).ShouldBeTrue();

    public class context : a_generated_invoice_application
    {
        protected override string InvoiceSource => when_rendering_portable_authorization.Source.Replace(
            "    slice StateView Lookup",
            "      " + """
            specification AllowingTheOwner
              given caller
                authenticated
                role "Staff"
                claim "owner" = "invoice-one"
                claim "region" = "North"
              when IssueInvoice
                invoiceId = "invoice-one"
                description = "North"
              then InvoiceIssued
                invoiceId = "invoice-one"
                description = "North"
            specification DenyingTheOtherOwner
              given caller
                authenticated
                role "Staff"
                claim "owner" = "invoice-two"
                claim "region" = "North"
              when IssueInvoice
                invoiceId = "invoice-one"
                description = "North"
              then denied
            slice StateView Lookup
            """.Replace("\n", "\n      ", StringComparison.Ordinal)
                .Replace("\n      slice StateView Lookup", "\n    slice StateView Lookup", StringComparison.Ordinal),
            StringComparison.Ordinal);

        Task Because() => VerifyGeneratedApplication();
    }
}
#endif
