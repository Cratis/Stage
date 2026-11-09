// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_named_sinks.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_named_sinks;

public class with_a_read_model_and_a_graph_in_named_sinks : Specification
{
    SemanticCompilation _compilation = null!;
    ArtifactRenderPlan _plan = null!;
    SemanticCompilation _withoutSinksCompilation = null!;
    ArtifactRenderPlan _withoutSinks = null!;

    void Establish() => _compilation = a_model_with_named_sinks.Compile(a_model_with_named_sinks.Sinks + a_model_with_named_sinks.Source);

    void Because()
    {
        _plan = a_model_with_named_sinks.Plan(_compilation);
        _withoutSinksCompilation = a_model_with_named_sinks.Compile(a_model_with_named_sinks.WithoutSinks);
        _withoutSinks = a_model_with_named_sinks.Plan(_withoutSinksCompilation);
    }

    string Compose => a_model_with_named_sinks.Text(_plan, "docker-compose.yml");

    [Fact] void should_render_successfully() => Assert.True(_plan.Success, string.Join("; ", _plan.Diagnostics));
    [Fact] void should_store_the_read_model_in_its_sink() => a_model_with_named_sinks.Text(_plan, "Directory.cs").ShouldContain("[Sink(\"Reporting\")]");
    [Fact] void should_store_the_graph_node_in_its_sink() => a_model_with_named_sinks.Text(_plan, "Graphs/Organization/Person.cs").ShouldContain("[Sink(\"People\")]");
    [Fact] void should_configure_the_read_model_sink_on_the_development_mongodb() => Compose.ShouldContain("      Cratis__Chronicle__Sinks__Configurations__Reporting__Type: MongoDB\n      Cratis__Chronicle__Sinks__Configurations__Reporting__ConnectionDetails: mongodb://localhost:27017\n");
    [Fact] void should_configure_the_graph_sink_on_the_development_neo4j() => Compose.ShouldContain("      Cratis__Chronicle__Sinks__Configurations__People__Type: Neo4j\n      Cratis__Chronicle__Sinks__Configurations__People__Server: bolt://neo4j:7687\n");
    [Fact] void should_name_the_neo4j_password_variable_and_not_its_value() => Compose.ShouldContain("Cratis__Chronicle__Sinks__Configurations__People__PasswordSecret: NEO4J_PASSWORD\n");
    [Fact] void should_give_each_service_one_environment() => Compose.Split("\n    environment:\n").Length.ShouldEqual(3);
    [Fact] void should_leave_the_model_revision_unchanged() => _compilation.Model.Revision.ShouldEqual(_withoutSinksCompilation.Model.Revision);
    [Fact] void should_change_the_render_input_hash() => a_model_with_named_sinks.InputDigest(a_model_with_named_sinks.Profile(_compilation)).ShouldNotEqual(a_model_with_named_sinks.InputDigest(a_model_with_named_sinks.Profile(_withoutSinksCompilation)));
    [Fact] void should_render_no_sink_without_a_directive() => a_model_with_named_sinks.Text(_withoutSinks, "Directory.cs").ShouldNotContain("Sink");
    [Fact] void should_configure_no_sink_without_a_declaration() => a_model_with_named_sinks.Text(_withoutSinks, "docker-compose.yml").ShouldNotContain("Sinks");
}
#endif
