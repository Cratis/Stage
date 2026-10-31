// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticImplementationAdmission.when_admitting_performers.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateViewArtifactRenderer.when_rendering_performed_queries;

public class with_a_live_performer_body_that_does_not_compile : Specification
{
    ArtifactRenderPlan _plan = null!;

    async Task Because()
    {
        var loaded = await when_rendering_a_pure_reducer.Load(a_performer.Source("return missingStream;"));
        _plan = when_rendering_a_pure_reducer.Plan(loaded);
    }

    [Fact] void should_leave_an_explicit_gap() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Partial);
    [Fact] void should_report_the_body_diagnostic() => Assert.Contains(_plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-IMPL-006" && diagnostic.Message.Contains("CS0103", StringComparison.Ordinal));
    [Fact] void should_report_the_gap() => Assert.Contains(_plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-IMPL-006");
    [Fact] void should_preserve_the_live_versions() => (_plan.ImplementationGaps.Single().ContextVersion, _plan.ImplementationGaps.Single().ResultVersion).ShouldEqual((1u, 2u));
    [Fact] void should_throw_instead_of_observing() => Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("/People.cs", StringComparison.Ordinal)).Bytes.AsSpan()).ShouldContain("throw new NotImplementedException(");
}
