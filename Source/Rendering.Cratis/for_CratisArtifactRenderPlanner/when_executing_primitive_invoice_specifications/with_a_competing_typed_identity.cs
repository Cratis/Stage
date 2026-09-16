// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

using context = Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_primitive_invoice_specifications.with_a_competing_typed_identity.context;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_primitive_invoice_specifications;

public class with_a_competing_typed_identity(context fixture) : IClassFixture<context>
{
    [Fact] void should_build_debug_without_warnings() => fixture.DebugWarnings.ShouldEqual(string.Empty);
    [Fact] void should_build_release_without_warnings() => fixture.ReleaseWarnings.ShouldEqual(string.Empty);
    [Fact] void should_execute_all_four_generated_facts() => fixture.Results.Length.ShouldEqual(4);
    [Fact] void should_pass_every_generated_fact() => fixture.Results.All(_ => _.Outcome == "Passed").ShouldBeTrue();
    [Fact] void should_verify_the_first_modeled_destination_instead_of_the_earlier_identity() => fixture.Results.Any(_ => _.Name.EndsWith(".when_issuing_first_invoice.should_have_appended_invoice_issued", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_verify_the_second_modeled_destination_instead_of_the_earlier_identity() => fixture.Results.Any(_ => _.Name.EndsWith(".when_issuing_second_invoice.should_have_appended_invoice_issued", StringComparison.Ordinal)).ShouldBeTrue();

    public class context : a_generated_invoice_application
    {
        protected override string InvoiceSource => invoice_model.WithCompetingIdentity();

        Task Because() => VerifyGeneratedApplication();
    }
}
#endif
