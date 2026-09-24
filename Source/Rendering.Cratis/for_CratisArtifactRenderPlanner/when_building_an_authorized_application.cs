// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_building_an_authorized_application : a_generated_invoice_application
{
    protected override string InvoiceSource => when_rendering_portable_authorization.Source + "\n" +
        invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource)
            .Replace("module Billing", "module OpenBilling", StringComparison.Ordinal)
            .Replace("IssueInvoice", "CreatePublicInvoice", StringComparison.Ordinal)
            .Replace("InvoiceIssued", "PublicInvoiceIssued", StringComparison.Ordinal);

    async Task Because() => await VerifyGeneratedApplication();

    [Fact] void should_build_debug_without_warnings() => DebugWarnings.ShouldBeEmpty();
    [Fact] void should_build_release_without_warnings() => ReleaseWarnings.ShouldBeEmpty();
    [Fact] void should_run_generated_specs() => Results.Select(result => result.Outcome).ShouldContainOnly(["Passed", "Passed", "Passed", "Passed"]);
}
#endif
