// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

using context = Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_primitive_invoice_specifications.with_prior_project_state.context;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_primitive_invoice_specifications;

public class with_prior_project_state(context fixture) : IClassFixture<context>
{
    [Fact] void should_build_debug_without_warnings() => fixture.DebugWarnings.ShouldEqual(string.Empty);
    [Fact] void should_build_release_without_warnings() => fixture.ReleaseWarnings.ShouldEqual(string.Empty);
    [Fact] void should_pass_generated_specifications() => fixture.Results.All(_ => _.Outcome == "Passed").ShouldBeTrue();
    [Fact] void should_run_the_subset_projection_assertion() => fixture.Results.Any(_ => _.Name.EndsWith("should_project_project_id", StringComparison.Ordinal)).ShouldBeTrue();

    public class context : a_generated_invoice_application
    {
        protected override string InvoiceSource => register_project_scenarios.WithPriorProjectAndSubsetExpectation;

        Task Because() => VerifyGeneratedApplication();
    }
}
#endif
