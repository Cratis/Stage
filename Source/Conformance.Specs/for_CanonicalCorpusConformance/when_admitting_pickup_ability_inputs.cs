// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Cratis.Stage.Contracts.Lifecycles;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance;

public class when_admitting_pickup_ability_inputs : Specification
{
    conformance_vector[] _vectors = [];
    WorkflowGraphSpec[] _graphs = [];
    conformance_observation[] _observations = [];

    void Establish()
    {
        string[] names = ["EsmGoldenVectors.V54", "EsmGoldenVectors.V55", "EsmGoldenVectors.V56"];
        _vectors = [.. canonical_corpus_vectors.All.Where(vector => names.Contains(vector.Name, StringComparer.Ordinal))];
    }

    async Task Because()
    {
        _graphs = [.. _vectors.Select(vector => SemanticExecutionPlan.Compile(vector.Model, []).Plan)
            .Select(plan => WorkflowGraphSpecs.For(plan).Single(graph => graph.Workflow.Name == "Pickup"))];
        _observations = await Task.WhenAll(_vectors.Select(vector => conformance_outcomes.Execute(conformance_surface.Host, vector)));
    }

    [Fact] void should_cover_all_three_golden_versions() => _graphs.Length.ShouldEqual(3);
    [Fact] void should_translate_ability_actions() => Assert.All(_graphs, graph => graph.Nodes.Any(node => node.Capability?.StartsWith(AbilityInputs.CapabilityPrefix, StringComparison.Ordinal) == true).ShouldBeTrue());
    [Fact] void should_refuse_the_compute_node_first() => Assert.All(_graphs, graph => Assert.Contains("Compute", graph.Refusals[0].Message, StringComparison.Ordinal));
    [Fact] void should_not_refuse_ability_inputs() => Assert.All(_graphs, graph => graph.Refusals.Any(refusal => refusal.Code == LifecycleCodes.AbilityNode).ShouldBeFalse());
    [Fact] void should_report_the_unknown_node_with_the_workflow_identity() => Assert.All(_observations, observation => observation.Refusals.Single(refusal => refusal.Kind == "workflow" && refusal.Artifact == "Pickup").ShouldEqual(new refusal_fingerprint(LifecycleCodes.UnknownNode, "workflow", "Pickup")));
}
#endif
