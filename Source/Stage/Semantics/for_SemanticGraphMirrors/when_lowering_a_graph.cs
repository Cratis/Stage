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

public class when_lowering_a_graph : Specification
{
    const string Source = """
        concept PersonId : Uuid
        concept PersonName : String

        module Organization
          feature Chart
            slice StateView OrganizationChart
              event PersonRegistered
                name PersonName

              graph Organization
                node Person
                  personId PersonId key
                  name     PersonName

              projection OrganizationChart => graph Organization
                node Person
                  from PersonRegistered key $eventSourceId
                    name = name
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

    [Fact] void should_lower_one_read_model_and_projection_per_graph() => _result.Count.ShouldEqual(1);
    [Fact] void should_name_the_read_model_after_the_graph() => _result[0].ReadModel.Type.Identifier.ShouldEqual("Organization");
    [Fact] void should_project_it_with_the_graph_sink() => _result[0].ReadModel.Sink.TypeId.ShouldEqual(SemanticGraphMirrors.Neo4jSinkTypeId);
    [Fact] void should_point_the_projection_at_the_read_model() => _result[0].Projection.ReadModel.ShouldEqual("Organization");
    [Fact] void should_mark_the_schema_as_a_graph() => JsonNode.Parse(_result[0].ReadModel.Schema)!["x-graph"]!.GetValue<bool>().ShouldBeTrue();
    [Fact] void should_name_the_key_of_the_node() => JsonNode.Parse(_result[0].ReadModel.Schema)!["properties"]!["Person"]!["x-graph-key"]!.GetValue<string>().ShouldEqual("personId");
    [Fact] void should_key_the_node_on_the_event_source() => _result[0].Projection.Graph.Nodes["Person"].From.Single().Value.Key.ShouldEqual("$eventSourceId");
    [Fact] void should_map_the_event_property() => _result[0].Projection.Graph.Nodes["Person"].From.Single().Value.Properties["name"].ShouldEqual("name");
    [Fact] void should_not_map_the_key_as_a_property() => _result[0].Projection.Graph.Nodes["Person"].From.Single().Value.Properties.ContainsKey("personId").ShouldBeFalse();
}
