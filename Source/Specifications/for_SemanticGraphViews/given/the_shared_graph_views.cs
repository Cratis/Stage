// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Testing.Graphs;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Specifications.for_SemanticGraphViews.given;

/// <summary>
/// The shared graph views fixtures, copied byte-identical from the cross-repository specification: the executable model of
/// <c language="text">Fixtures/graph-views-organization.play</c>, which declares the fixture's views with <c language="screenplay">collect</c>, and the seed graph of the
/// shared query vectors in Chronicle's in-memory graph, whose query executor is the reference every graph view executor matches.
/// </summary>
public class the_shared_graph_views : Specification
{
    protected static readonly string Fixtures = Path.Combine(AppContext.BaseDirectory, "for_SemanticGraphViews", "Fixtures");

    protected SemanticApplication _application = null!;
    protected GraphScenario _scenario = null!;

    protected static JsonNode Fixture(string name) => JsonNode.Parse(File.ReadAllText(Path.Combine(Fixtures, name)))!;

    protected SemanticReadModel ReadModelOf(SemanticGraphQuery query) =>
        _application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).SelectMany(slice => slice.ReadModels).Single(readModel => readModel.Id == query.ReadModel);

    async Task Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Organization"));
        var source = await File.ReadAllTextAsync(Path.Combine(Fixtures, "graph-views-organization.play"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("organization"), "organization", "graph-views-organization.play", source);
        var compilation = new SemanticModelCompiler().Compile("Organization", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        _application = compilation.Value.Model.Application;

        var vectors = Fixture("graph-views-query-vectors.json");
        _scenario = new GraphScenario();
        foreach (var node in vectors["nodes"]!.AsArray())
        {
            await _scenario.SeedNode(node!["label"]!.GetValue<string>(), node["key"]!.GetValue<string>(), Properties(node["properties"]));
        }

        foreach (var edge in vectors["edges"]!.AsArray())
        {
            await _scenario.SeedEdge(
                edge!["type"]!.GetValue<string>(),
                edge["source"]!["key"]!.GetValue<string>(),
                edge["target"]!["key"]!.GetValue<string>(),
                edge["identity"]?.GetValue<string>(),
                Properties(edge["properties"]));
        }
    }

    void Destroy() => _scenario.Dispose();

    static Dictionary<string, object?> Properties(JsonNode? properties) =>
        properties?.AsObject().ToDictionary(property => property.Key, property => property.Value?.GetValueKind() == JsonValueKind.String ? (object?)property.Value.GetValue<string>() : null) ?? [];
}
