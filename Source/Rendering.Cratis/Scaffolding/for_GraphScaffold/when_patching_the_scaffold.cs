// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Scaffolding.for_GraphScaffold;

public class when_patching_the_scaffold : Specification
{
    Dictionary<string, string> _original = [];
    Dictionary<string, string> _patched = [];

    void Because()
    {
        var scaffold = new CratisBackendApplicationScaffold().Create(CratisBackendApplicationScaffoldRequest.Create("Organization"));
        var artifacts = scaffold.Select(input => CratisArtifactRenderInput.TryCreateArtifact(input, out var artifact) ? artifact! : throw new InvalidOperationException(input.Name)).ToArray();
        _original = artifacts.ToDictionary(artifact => artifact.RelativePath, artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan()), StringComparer.Ordinal);
        _patched = artifacts.Select(artifact => GraphScaffold.Patch(artifact, "Organization")).ToDictionary(artifact => artifact.RelativePath, artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan()), StringComparer.Ordinal);
    }

    [Fact] void should_add_neo4j_to_the_arc_builder() => _patched["Program.cs"].ShouldContain("arc => arc.WithMongoDB().WithNeo4j(),");
    [Fact] void should_import_the_extension_namespaces() => _patched["Program.cs"].ShouldContain("using Cratis.Arc;\nusing Cratis.Arc.MongoDB;\nusing Cratis.Arc.Neo4j;");
    [Fact] void should_reference_the_package_beside_mongodb() => _patched["Organization.csproj"].ShouldContain("<PackageReference Include=\"Cratis.Arc.MongoDB\" />\n    <PackageReference Include=\"Cratis.Arc.Neo4j\" />");
    [Fact] void should_pin_the_package_at_the_arc_release_stage_pins() => _patched["Directory.Packages.props"].ShouldContain($"<PackageVersion Include=\"Cratis.Arc.Neo4j\" Version=\"{ScaffoldPackageVersions.ArcNeo4j}\" />");
    [Fact] void should_configure_the_connection_without_a_password() => _patched["appsettings.json"].ShouldContain("\"Neo4j\": {\n      \"Server\": \"bolt://localhost:7687\",\n      \"EventStore\": \"Organization\",\n      \"UserName\": \"neo4j\",\n      \"PasswordSecret\": \"NEO4J_PASSWORD\",\n      \"DatabaseMode\": \"Single\"\n    },");
    [Fact] void should_read_the_graph_of_the_event_store_chronicle_writes() => _patched["appsettings.json"].ShouldContain("\"EventStore\": \"Organization\"");
    [Fact] void should_keep_the_settings_valid_json() => System.Text.Json.JsonDocument.Parse(_patched["appsettings.json"]).RootElement.GetProperty("Cratis").GetProperty("Neo4j").GetProperty("PasswordSecret").GetString().ShouldEqual("NEO4J_PASSWORD");
    [Fact] void should_add_a_development_neo4j_server() => _patched["docker-compose.yml"].ShouldContain("  neo4j:\n    image: neo4j:5-community");
    [Fact] void should_take_the_password_from_the_environment_of_the_person_running_it() => _patched["docker-compose.yml"].ShouldContain("NEO4J_AUTH: neo4j/${NEO4J_PASSWORD:?Set NEO4J_PASSWORD before starting the services}");
    [Fact] void should_tell_chronicle_where_the_graph_sink_connects() => _patched["docker-compose.yml"].ShouldContain("Cratis__Chronicle__Neo4j__Server: bolt://neo4j:7687");
    [Fact] void should_tell_chronicle_which_variable_holds_the_password() => _patched["docker-compose.yml"].ShouldContain("Cratis__Chronicle__Neo4j__PasswordSecret: NEO4J_PASSWORD");
    [Fact] void should_wait_for_neo4j_before_chronicle_starts() => _patched["docker-compose.yml"].ShouldContain("depends_on:\n      neo4j:\n        condition: service_healthy");
    [Fact] void should_keep_the_graph_across_restarts() => _patched["docker-compose.yml"].ShouldContain("  neo4j-data:\n");
    [Fact] void should_keep_the_chronicle_volumes() => _patched["docker-compose.yml"].ShouldContain("volumes:\n  chronicle-data:\n  neo4j-data:\n  chronicle-config:");
    [Fact] void should_explain_the_graph_in_the_readme() => _patched["README.md"].ShouldContain("## Graphs");
    [Fact] void should_keep_the_readme_sections_apart() => _patched["README.md"].ShouldContain("it.\n\n## Customize");
    [Fact] void should_not_write_a_credential_anywhere() => _patched.Values.ShouldEachConformTo(text => !text.Contains("neo4j/neo4j", StringComparison.Ordinal) && !text.Contains("\"Password\"", StringComparison.Ordinal));
    [Fact] void should_leave_the_other_files_alone() => _patched.Where(entry => entry.Key is not ("Program.cs" or "Organization.csproj" or "Directory.Packages.props" or "appsettings.json" or "docker-compose.yml" or "README.md")).ShouldEachConformTo(entry => entry.Value == _original[entry.Key]);
}
