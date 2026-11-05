// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Semantics.Graphs.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Graphs.for_GraphPlanning;

public class when_planning_a_model_with_a_graph_once_chronicle_has_the_graph_api : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        _plan = new CratisArtifactRenderPlanner().Plan(an_organization_graph_model.Request());
    }

    string Text(string path) => Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath == path).Bytes.AsSpan());

    [Fact] void should_plan_the_application() => Assert.True(_plan.Success, string.Join("; ", _plan.Diagnostics.Where(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).Select(diagnostic => $"{diagnostic.Code} {diagnostic.Message}")));
    [Fact] void should_not_refuse_the_graph() => _plan.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == ChronicleGraphSupport.NeedsChronicleGraphs);
    [Fact] void should_plan_a_record_per_node_and_edge() => _plan.Artifacts.Where(artifact => artifact.RelativePath.StartsWith("Graphs/Organization/", StringComparison.Ordinal)).Select(artifact => Path.GetFileName(artifact.RelativePath)).Order(StringComparer.Ordinal).ShouldEqual(["Group.cs", "HasRole.cs", "MemberOf.cs", "Person.cs"]);
    [Fact] void should_plan_the_specifications_that_the_graph_scenario_can_express() => _plan.Artifacts.Count(artifact => artifact.RelativePath.StartsWith("Graphs/Specifications/", StringComparison.Ordinal)).ShouldEqual(2);
    [Fact] void should_report_the_omitted_specification_as_a_degradation() => _plan.Degradations.ShouldContain(degradation => degradation.Diagnostic.Message.Contains("RemovingAPersonDetachesTheirEdges", StringComparison.Ordinal));
    [Fact] void should_wire_arc_to_neo4j_in_the_host() => Text("Program.cs").ShouldContain("arc => arc.WithMongoDB().WithNeo4j(),");
    [Fact] void should_reference_the_neo4j_package() => Text("Organization.csproj").ShouldContain("<PackageReference Include=\"Cratis.Arc.Neo4j\" />");
    [Fact] void should_pin_the_neo4j_package_centrally() => Text("Directory.Packages.props").ShouldContain($"<PackageVersion Include=\"Cratis.Arc.Neo4j\" Version=\"{Scaffolding.ScaffoldPackageVersions.ArcNeo4j}\" />");
    [Fact] void should_name_the_environment_variable_that_holds_the_password() => Text("appsettings.json").ShouldContain("\"PasswordSecret\": \"NEO4J_PASSWORD\"");
    [Fact] void should_not_write_a_password() => Text("appsettings.json").ShouldNotContain("\"Password\"");
}
#endif
