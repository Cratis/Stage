// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

using context = Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_primitive_invoice_specifications.with_text_destinations.context;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_primitive_invoice_specifications;

public class with_text_destinations(context fixture) : IClassFixture<context>
{
    [Fact] void should_build_debug_without_warnings() => fixture.DebugWarnings.ShouldEqual(string.Empty);
    [Fact] void should_build_release_without_warnings() => fixture.ReleaseWarnings.ShouldEqual(string.Empty);
    [Fact] void should_execute_all_four_generated_facts() => fixture.Results.Length.ShouldEqual(4);
    [Fact] void should_pass_every_generated_fact() => fixture.Results.All(_ => _.Outcome == "Passed").ShouldBeTrue();
    [Fact] void should_preserve_the_explicit_root_namespace() => fixture.Results.All(_ => _.Name.StartsWith("Invoices.", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_execute_both_commands() => fixture.Results.Count(_ => _.Name.EndsWith(".should_succeed", StringComparison.Ordinal)).ShouldEqual(2);
    [Fact] void should_verify_the_first_destination_and_payload() => fixture.Results.Any(_ => _.Name.EndsWith(".when_issuing_first_invoice.should_have_appended_invoice_issued", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_verify_the_second_destination_and_payload() => fixture.Results.Any(_ => _.Name.EndsWith(".when_issuing_second_invoice.should_have_appended_invoice_issued", StringComparison.Ordinal)).ShouldBeTrue();

    public class context : a_generated_invoice_application
    {
        protected override string InvoiceSource => invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource);

        Task Because() => VerifyGeneratedApplication();
    }
}
#endif
