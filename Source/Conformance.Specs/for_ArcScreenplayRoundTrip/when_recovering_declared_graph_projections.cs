// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Arc.Screenplay;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics.Graphs.given;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Cratis.Stage.Conformance.for_ArcScreenplayRoundTrip;

public class when_recovering_declared_graph_projections : Specification
{
    ExecutableSemanticModel _original = null!;
    ExecutableSemanticModel _recovered = null!;
    ExecutableSemanticModel _regrouped = null!;
    ExecutableSemanticModel _separate = null!;

    void Because()
    {
        _original = an_organization_graph_model.Request(graph_projection_sources.Split).Model;
        _recovered = Recover(graph_projection_sources.Split);
        _regrouped = Recover(graph_projection_sources.Regrouped);
        _separate = Recover(graph_projection_sources.SeparateSlice);
    }

    static ExecutableSemanticModel Recover(string source)
    {
        var request = an_organization_graph_model.Request(source);
        var rendered = CratisRendering.Plan(request.Model, request.ExecutionPlan, request.Scope, new("Organization", "Organization"));
        Assert.True(rendered.Success, string.Join(Environment.NewLine, rendered.Diagnostics));
        var files = rendered.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs" && !artifact.RelativePath.StartsWith("Frontend/", StringComparison.Ordinal)).ToArray();
        var compilation = RenderedOutput.CreateCompilation(files.Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan()))));
        Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var result = new ScreenplayGenerator().Generate(compilation, new() { Domain = "Organization", Module = "Organization", SegmentsToSkip = 1 });
        Assert.True(result.IsSuccess, string.Join(Environment.NewLine, result.Diagnostics));
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Code == "SP0089");
        return an_organization_graph_model.Request(result.Source).Model;
    }

    static IEnumerable<string> Inventory(ExecutableSemanticModel model) => model.Application.GraphProjections.OrderBy(projection => projection.Name, StringComparer.Ordinal).Select(projection =>
        $"{projection.Name}|{projection.Graph}|{string.Join('/', GeneratedTypeNames.AllSlices(model.Application).Single(slice => slice.Slice.Id == projection.DeclaringSlice).Path)}|{string.Join(',', projection.Nodes.Select(node => node.Name).Order(StringComparer.Ordinal))}|{string.Join(',', projection.Edges.Select(edge => edge.Name).Order(StringComparer.Ordinal))}");

    static IEnumerable<string> Handlers(ExecutableSemanticModel model) => model.Application.GraphProjections
        .SelectMany(projection => projection.Nodes.Concat(projection.Edges)).OrderBy(element => element.Name, StringComparer.Ordinal)
        .Select(element => $"{element.Name}:{string.Join(',', element.Handlers.Select(handler => $"{handler.Removes}/{handler.Detach}/{handler.Mappings.Length}"))}");

    [Fact] void should_preserve_names_graph_members_and_nested_declaring_slices() => Inventory(_recovered).ShouldEqual(Inventory(_original));
    [Fact] void should_preserve_projection_handler_sets() => Handlers(_recovered).ShouldEqual(Handlers(_original));
    [Fact] void should_not_collapse_two_projections_in_one_slice() => _recovered.Application.GraphProjections.Length.ShouldEqual(2);
    [Fact] void should_recover_a_different_grouping_of_the_same_execution_records() => _regrouped.Application.GraphProjections.Single().Nodes.Length.ShouldEqual(2);
    [Fact] void should_keep_the_regrouped_edge_with_its_declared_projection() => _regrouped.Application.GraphProjections.Single().Edges.Single().Name.ShouldEqual("MEMBER_OF");
    [Fact] void should_preserve_handlers_after_regrouping() => Handlers(_regrouped).ShouldEqual(Handlers(_original));
    [Fact] void should_preserve_a_projection_declared_in_a_different_slice() => _separate.Application.GraphProjections.Select(projection => projection.DeclaringSlice).Distinct().Count().ShouldEqual(2);
    [Fact] void should_retain_a_shape_only_node_without_assigning_it_to_a_projection() => _recovered.Application.Graphs.Single().Nodes.Single(node => node.Label == "Unhandled").Properties.Length.ShouldEqual(1);
    [Fact] void should_not_assign_the_shape_only_node() => _recovered.Application.GraphProjections.SelectMany(projection => projection.Nodes).Any(node => node.Name == "Unhandled").ShouldBeFalse();
}
#endif
