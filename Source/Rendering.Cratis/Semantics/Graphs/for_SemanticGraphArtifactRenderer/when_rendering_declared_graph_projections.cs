// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Semantics.Graphs.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Graphs.for_SemanticGraphArtifactRenderer;

public class when_rendering_declared_graph_projections : Specification
{
    SemanticApplicationContext _context = null!;
    RenderedFile[] _split = [];
    RenderedFile[] _regrouped = [];
    RenderedFile[] _legacy = [];
    RenderedFile[] _separate = [];
    RenderedFile[] _reordered = [];
    IReadOnlyList<string> _errors = [];

    void Because()
    {
        _context = an_organization_graph_model.Context(graph_projection_sources.Split);
        _split = [.. SemanticGraphArtifactRenderer.Render(_context)];
        _regrouped = [.. SemanticGraphArtifactRenderer.Render(an_organization_graph_model.Context(graph_projection_sources.Regrouped))];
        _separate = [.. SemanticGraphArtifactRenderer.Render(an_organization_graph_model.Context(graph_projection_sources.SeparateSlice))];
        var request = _context.Request;
        var reordered = ExecutableSemanticModel.Create(request.Model.LanguageVersion, request.Model.SemanticVersion, request.Model.Application with { GraphProjections = [.. request.Model.Application.GraphProjections.Reverse()] });
        var reorderedRequest = request with { Model = reordered, ExecutionPlan = SemanticExecutionPlan.Compile(reordered).Plan };
        _reordered = [.. SemanticGraphArtifactRenderer.Render(new(reorderedRequest, new("Organization", "Organization")))];
        var application = request.Model.Application with { GraphProjections = [.. request.Model.Application.GraphProjections.Select(projection => projection with { DeclaringSlice = null })] };
        var legacy = ExecutableSemanticModel.Create(LanguageVersion.V38, SemanticVersion.V38, application);
        var legacyRequest = request with { Model = legacy, ExecutionPlan = SemanticExecutionPlan.Compile(legacy).Plan };
        _legacy = [.. SemanticGraphArtifactRenderer.Render(new(legacyRequest, new("Organization", "Organization")))];
        var plan = CratisRendering.Plan(request.Model, request.ExecutionPlan, request.Scope, new("Organization", "Organization"));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        _errors = RenderedOutput.Errors(plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs" && !artifact.RelativePath.StartsWith("Frontend/", StringComparison.Ordinal))
            .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan()))));
    }

    static IEnumerable<RenderedFile> Declarations(IEnumerable<RenderedFile> files) => files.Where(file => !file.RelativePath.StartsWith("Graphs/", StringComparison.Ordinal));
    static IEnumerable<(string, string)> Records(IEnumerable<RenderedFile> files) => files.Where(file => file.RelativePath.StartsWith("Graphs/", StringComparison.Ordinal)).Select(file => (file.RelativePath, file.Content));
    string Declaration(int index) => Declarations(_split).ElementAt(index).Content;

    [Fact] void should_use_the_declaring_slice_version() => _context.Request.Model.SemanticVersion.ShouldEqual(SemanticVersion.V60);
    [Fact] void should_compile_against_the_real_arc_attribute() => _errors.ShouldBeEmpty();
    [Fact] void should_preserve_two_projections_in_one_slice() => Declarations(_split).Count().ShouldEqual(2);
    [Fact] void should_use_the_nested_declaring_slice_instead_of_the_event_slice() => Declarations(_split).ShouldEachConformTo(file => file.RelativePath.StartsWith("Organization/Chart/Views/ProjectionHost/", StringComparison.Ordinal));
    [Fact] void should_use_the_declaring_namespace() => Declaration(0).ShouldContain("namespace Organization.Organization.Chart.Views.ProjectionHost;");
    [Fact] void should_keep_the_original_name_independent_of_the_slice() => Declaration(0).ShouldContain("GraphProjectionDeclaration(\"PeopleIndex\", \"Organization\"");
    [Fact] void should_preserve_the_other_original_name() => Declaration(1).ShouldContain("GraphProjectionDeclaration(\"MembershipIndex\", \"Organization\"");
    [Fact] void should_import_the_graph_records() => Declaration(0).ShouldContain("using Organization.Graphs.Organization;");
    [Fact] void should_group_only_the_owned_nodes() => Declaration(0).ShouldContain("typeof(Person), typeof(Group)");
    [Fact] void should_not_assign_an_edge_to_the_node_projection() => Declaration(0).ShouldNotContain("typeof(MemberOf)");
    [Fact] void should_group_only_the_owned_edges() => Declaration(1).ShouldContain("typeof(MemberOf)");
    [Fact] void should_not_assign_shape_only_nodes() => string.Join('\n', Declarations(_split).Select(file => file.Content)).ShouldNotContain("typeof(Unhandled)");
    [Fact] void should_associate_provenance_with_the_declaring_slice() => Declarations(_split).ShouldEachConformTo(file => file.Sources.SequenceEqual([_context.Application.GraphProjections[0].DeclaringSlice!.Value]));
    [Fact] void should_register_the_marker_types() => _context.GeneratedTypes.Where(type => type.Name.EndsWith("GraphProjection", StringComparison.Ordinal)).Select(type => type.Name).Order(StringComparer.Ordinal).ShouldEqual("MembershipIndexGraphProjection", "PeopleIndexGraphProjection");
    [Fact] void should_use_meaningful_marker_file_names() => Declarations(_split).Select(file => Path.GetFileName(file.RelativePath)).ShouldEqual("PeopleIndexGraphProjection.cs", "MembershipIndexGraphProjection.cs");
    [Fact] void should_declare_the_meaningful_marker_class() => Declaration(0).ShouldContain("public sealed class PeopleIndexGraphProjection;");
    [Fact] void should_keep_marker_names_when_declaration_order_changes() => Declarations(_reordered).Select(file => file.RelativePath).Order(StringComparer.Ordinal).ShouldEqual(Declarations(_split).Select(file => file.RelativePath).Order(StringComparer.Ordinal));
    [Fact] void should_distinguish_a_regrouped_projection() => Declarations(_regrouped).Single().Content.ShouldContain("typeof(MemberOf)");
    [Fact] void should_keep_execution_unchanged_when_regrouping() => Records(_regrouped).ShouldEqual(Records(_split));
    [Fact] void should_keep_execution_unchanged_for_legacy_models() => Records(_legacy).ShouldEqual(Records(_split));
    [Fact] void should_not_invent_legacy_projection_ownership() => Declarations(_legacy).ShouldBeEmpty();
    [Fact] void should_distinguish_placement_in_another_slice() => Declarations(_separate).Last().RelativePath.StartsWith("Organization/Chart/Views/MembershipHost/", StringComparison.Ordinal).ShouldBeTrue();
}
#endif
