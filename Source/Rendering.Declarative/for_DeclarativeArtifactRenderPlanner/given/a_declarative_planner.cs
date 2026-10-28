// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Kits;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.given;

/// <summary>
/// Builds the test kit, the render request and the planner that specs drive.
/// </summary>
public class a_declarative_planner : Specification
{
    internal const string TwoSlices = """
        module Messaging
          feature Echoes
            slice StateChange Reply
              command Echo
                message String
                returns message
              command Shout
                message String
                returns message
            slice Automation Notify
              reaction EchoReaction
                when Startup
        """;

    internal const string Manifest = """
        {"apiVersion":"stage-target/v1.0","target":{"id":"acme","version":"1.0.0"},
        "languagePacks":["test@1.0.0"],"requires":["templates/v1.0","model/v1.0"],
        "outputs":[
          {"id":"command","template":"templates/command.sbn","path":"Commands/{{ item.name }}.test","forEach":"model.items.commands"},
          {"id":"reaction","template":"templates/reaction.sbn","path":"Reactions/{{ item.name }}.test","forEach":"model.items.reactions"},
          {"id":"index","template":"templates/index.sbn","path":"Index.test"}],
        "partials":[{"name":"header","template":"templates/header.sbn"}],
        "tables":[],"assets":[],"diagnostics":[]}
        """;

    internal static ImmutableArray<byte> Bytes(string text) => [.. Encoding.UTF8.GetBytes(text)];

    internal static Dictionary<string, ImmutableArray<byte>> KitFiles(string manifest = Manifest) => new(StringComparer.Ordinal)
    {
        [KitLoader.ManifestPath] = Bytes(manifest),
        ["templates/command.sbn"] = Bytes("command {{ item.name }}"),
        ["templates/reaction.sbn"] = Bytes("reaction {{ item.name }}"),
        ["templates/index.sbn"] = Bytes("index"),
        ["templates/header.sbn"] = Bytes("header")
    };

    internal static ImmutableArray<byte> Archive(Dictionary<string, ImmutableArray<byte>>? files = null) => KitArchive.Write(new(files ?? KitFiles()));

    internal static ArtifactRenderProfile Profile(ImmutableArray<byte> kit, string renderer = DeclarativeArtifactRenderPlanner.Renderer, string target = "acme", string version = "1.0.0", params ArtifactRenderInput[] extra) =>
        ArtifactRenderProfile.Create(target, version, renderer, DeclarativeArtifactRenderPlanner.RendererVersion, [ArtifactRenderInput.Create("kit", "1.0.0", kit), .. extra]);

    internal static ArtifactRenderRequest Request(ArtifactRenderProfile profile, string source = TwoSlices)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Sample"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "model.play", source);
        var compilation = new SemanticModelCompiler().Compile("Sample", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;
        return new(model, SemanticExecutionPlan.Compile(model).Plan!, profile, new(ArtifactRenderScopeKind.Application, model.Application.Id));
    }

    internal static DeclarativeArtifactRenderPlanner Planner(params IDeclarativeLanguage[] languages) => new(languages.Length == 0 ? [new a_test_language()] : languages);

    internal static ArtifactRenderPlan Plan(ArtifactRenderProfile profile, params IDeclarativeLanguage[] languages) => Planner(languages).Plan(Request(profile));
}
