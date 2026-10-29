// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

using context = Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_opaque_policy_specifications.with_an_authored_clock.context;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_opaque_policy_specifications;

public class with_an_authored_clock(context fixture) : IClassFixture<context>
{
    [Fact] void should_build_debug_without_warnings() => fixture.DebugWarnings.ShouldEqual(string.Empty);
    [Fact] void should_build_release_without_warnings() => fixture.ReleaseWarnings.ShouldEqual(string.Empty);
    [Fact] void should_execute_both_scenarios() => fixture.Results.Length.ShouldBeGreaterThan(5);
    [Fact] void should_pass_generated_assertions() => fixture.Results.All(result => result.Outcome == "Passed").ShouldBeTrue();
    [Fact] void should_assert_denial_outside_the_authored_year() => fixture.Results.Any(result => result.Name.EndsWith("should_be_denied", StringComparison.Ordinal)).ShouldBeTrue();

    public class context : a_generated_invoice_application
    {
        protected override string InvoiceSource => opaque_policy_specification_model.ClockSource();
        protected override ArtifactRenderPlan CreatePlan() => opaque_policy_model.Plan(InvoiceSource);

        Task Because() => VerifyGeneratedApplication();
    }
}
#endif
