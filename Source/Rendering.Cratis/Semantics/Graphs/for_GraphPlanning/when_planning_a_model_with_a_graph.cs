// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics.Graphs.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Graphs.for_GraphPlanning;

public class when_planning_a_model_with_a_graph : Specification
{
    ArtifactRenderPlan _application = null!;
    ArtifactRenderPlan _module = null!;
    ArtifactRenderPlan _clock = null!;

    void Because()
    {
        var request = an_organization_graph_model.Request();
        var planner = new CratisArtifactRenderPlanner();
        _application = planner.Plan(request);
        _module = planner.Plan(request with { Scope = new(ArtifactRenderScopeKind.Module, request.Model.Application.Modules[0].Id) });
        _clock = planner.Plan(an_organization_graph_model.Request("time zone configurable default \"Europe/Oslo\"\n\n" + an_organization_graph_model.Source));
    }

    [Fact] void should_admit_the_esm_v38_pair() => _application.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == "STAGE-ESM-016");
    [Fact] void should_not_refuse_the_graph_with_the_pinned_chronicle_client() => _application.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == ChronicleGraphSupport.NeedsChronicleGraphs);
    [Fact] void should_plan_the_application_with_it() => _application.Success.ShouldBeTrue();
    [Fact] void should_plan_the_graph() => _application.Artifacts.ShouldContain(artifact => artifact.RelativePath.StartsWith("Graphs/", StringComparison.Ordinal));
    [Fact] void should_name_the_graph_constructs_the_application_scope_leaves_out_of_a_module_plan() => _module.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == SemanticGraphAdmission.ScopedOut && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Information);
    [Fact] void should_not_refuse_a_module_plan_for_the_graph() => _module.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == ChronicleGraphSupport.NeedsChronicleGraphs);
    [Fact] void should_still_refuse_the_v37_clock_constructs_inside_a_v38_model() => _clock.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-016" && diagnostic.Message.Contains("TimeZone", StringComparison.Ordinal));
}
#endif
