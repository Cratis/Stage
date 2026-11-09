// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Xunit;
using ChronicleProjections = Cratis.Chronicle.Contracts.Projections;
using ChronicleReadModels = Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Stage.Semantics.for_SemanticGraphMirrors;

public class when_lowering_a_graph_in_a_named_sink : Specification
{
    const string Source = """
        sink People for graphs

        concept PersonId : Uuid
        concept PersonName : String

        module Organization
          feature Chart
            slice StateView OrganizationChart
              event PersonRegistered
                name PersonName

              graph Organization
                sink People
                node Person
                  personId PersonId key
                  name     PersonName

              projection OrganizationChart => graph Organization
                node Person
                  from PersonRegistered key $eventSourceId
                    name = name
        """;

    IReadOnlyList<(ChronicleReadModels.ReadModelDefinition ReadModel, ChronicleProjections.ProjectionDefinition Projection)> _result = [];
    IReadOnlyList<(ChronicleReadModels.ReadModelDefinition ReadModel, ChronicleProjections.ProjectionDefinition Projection)> _default = [];

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Model"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "Model.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Model", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success);
        var model = ExecutableSemanticModel.Create(compilation.Value.Model.LanguageVersion, compilation.Value.Model.SemanticVersion, compilation.Value.Model.Application);
        var plan = SemanticExecutionPlan.Compile(model).Plan;
        _result = SemanticGraphMirrors.Lower(plan, "InMemoryGraph", compilation.Value.GraphSinks);
        _default = SemanticGraphMirrors.Lower(plan, "InMemoryGraph");
    }

    [Fact] void should_register_the_graph_with_its_sink_name() => _result[0].ReadModel.Sink.Name.ShouldEqual("People");
    [Fact] void should_keep_the_sink_type_the_deployment_chose() => _result[0].ReadModel.Sink.TypeId.ShouldEqual("InMemoryGraph");
    [Fact] void should_register_a_graph_without_a_sink_name_in_the_default_sink() => _default[0].ReadModel.Sink.Name.ShouldEqual(string.Empty);
}
