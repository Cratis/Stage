// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Xunit;
using ChronicleProjections = Cratis.Chronicle.Contracts.Projections;
using ChronicleReadModels = Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Stage.Semantics.for_SemanticGraphMirrors;

public class when_lowering_a_graph_that_iterates_a_collection : Specification
{
    const string Source = """
        concept ModelId : Uuid
        concept SeriesId : Uuid
        concept SlotName : String

        type ModelSlot
          slot   SlotName
          series SeriesId

        module Scoring
          feature Models
            slice StateView ModelScores
              event ModelDefined
                slots ModelSlot[]

              event ModelRetired
                slots ModelSlot[]

              graph Scoring
                node Model
                  modelId ModelId key
                node Series
                  seriesId SeriesId key
                edge SCORES source Model target Series identified by slot
                  slot SlotName

              projection ModelScores => graph Scoring
                node Model
                  from ModelDefined key $eventSourceId
                node Series
                  from ModelDefined foreach slots key slots.series
                edge SCORES
                  from ModelDefined foreach slots source $eventSourceId target slots.series identified by slots.slot
                    slot = slots.slot
                  remove with ModelRetired foreach slots source $eventSourceId target slots.series identified by slots.slot
        """;

    IReadOnlyList<(ChronicleReadModels.ReadModelDefinition ReadModel, ChronicleProjections.ProjectionDefinition Projection)> _result = [];

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Model"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "Model.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Model", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success);
        var model = ExecutableSemanticModel.Create(compilation.Value.Model.LanguageVersion, compilation.Value.Model.SemanticVersion, compilation.Value.Model.Application);
        _result = SemanticGraphMirrors.Lower(SemanticExecutionPlan.Compile(model).Plan);
    }

    ChronicleProjections.GraphEdgeFromDefinition Scores => _result[0].Projection.Graph.Edges["SCORES"].From.Single().Value;

    [Fact] void should_name_the_collection_a_node_mapping_iterates() => _result[0].Projection.Graph.Nodes["Series"].From.Single().Value.ForEach.ShouldEqual("slots");
    [Fact] void should_not_iterate_a_node_mapping_that_does_not_say_so() => _result[0].Projection.Graph.Nodes["Model"].From.Single().Value.ForEach.ShouldBeNull();
    [Fact] void should_address_the_item_through_the_collection_in_a_key() => _result[0].Projection.Graph.Nodes["Series"].From.Single().Value.Key.ShouldEqual("slots.series");
    [Fact] void should_name_the_collection_an_edge_mapping_iterates() => Scores.ForEach.ShouldEqual("slots");
    [Fact] void should_address_the_item_in_the_target() => Scores.TargetKey.ShouldEqual("slots.series");
    [Fact] void should_address_the_item_in_the_identity() => Scores.Identity.ShouldEqual("slots.slot");
    [Fact] void should_keep_the_event_source_as_the_source() => Scores.SourceKey.ShouldEqual("$eventSourceId");
    [Fact] void should_name_the_collection_an_edge_removal_iterates() => _result[0].Projection.Graph.Edges["SCORES"].RemovedWith.Single().Value.ForEach.ShouldEqual("slots");
}
