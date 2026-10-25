// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationArtifactRenderer;

public class when_rendering_protected_command_specifications : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var model = SemanticModelSerializer.Deserialize(ProtectedCommandReadsCorpus.V25.EsmBytes.AsSpan());
        _plan = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("ProtectedReads", "ProtectedReads"));
    }

    [Fact] void should_seed_prior_facts_through_the_scenario_given() => Source("when_missing_after_removal").ShouldContain("_scenario.Given.ForEventSource(_sourceId).Events(");
    [Fact] void should_not_use_the_event_scenario_that_decision_mode_disables() => Source("when_missing_after_removal").ShouldNotContain("EventScenario");
    [Fact] void should_not_declare_an_async_establish_without_an_await() => Source("when_missing_after_removal").ShouldContain("void Establish()");

    // Screenplay 4.246 (decision 0115) states the reaction in its own Workflow slice as an append: no command cascade is left to split.
    [Fact] void should_render_the_reaction_as_one_append_specification() => _plan.Artifacts.Count(artifact => artifact.RelativePath.EndsWith("/when_reaction_invokes_protected_read.cs", StringComparison.Ordinal) || artifact.RelativePath.EndsWith("/when_reaction_invokes_protected_read_trigger.cs", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_execute_the_invoked_command_after_the_reactor_returns_it() => Source("/when_reaction_invokes_protected_read.cs").ShouldContain("_reactor.Produced.OfType<global::ProtectedReads.Storage.Decisions.Use.Consume>()");
    [Fact] void should_decide_the_command_another_slice_owns() => Source("/when_reaction_invokes_protected_read.cs").ShouldContain("void should_succeed() => _result.ShouldBeSuccessful();");
    [Fact] void should_not_assert_the_facts_of_the_command_another_slice_owns() => Source("/when_reaction_invokes_protected_read.cs").ShouldNotContain("should_append");

    string Source(string name) => string.Concat(_plan.Artifacts.Where(artifact => artifact.RelativePath.Contains(name, StringComparison.Ordinal)).Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
}
