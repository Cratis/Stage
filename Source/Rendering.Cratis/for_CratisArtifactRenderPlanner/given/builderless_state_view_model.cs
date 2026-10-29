// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

/// <summary>
/// Plans state views whose read models nothing builds, like the navigation sample's WelcomeInfo.
/// </summary>
public static class builderless_state_view_model
{
    public const string Builderless = """
        module Home
          feature Welcome
            slice StateView WelcomeInfo
              readmodel WelcomeRow
                id Uuid key
                text String
              screen Welcome
                title "Welcome"
        """;

    public const string Queried = """
        module Home
          feature Welcome
            slice StateView WelcomeInfo
              readmodel WelcomeRow
                id Uuid
                text String
              query ById => WelcomeRow?
                by id Uuid
        """;

    public const string BuiltQuery = """
        module Home
          feature Welcome
            slice StateChange Welcome
              command Welcome
                id Uuid identifier
                text String
                produces Welcomed
                  for id
                  id = id
                  text = text
              event Welcomed
                id Uuid
                text String
            slice StateView WelcomeInfo
              readmodel WelcomeRow
                id Uuid
                text String
              query ById => WelcomeRow?
                by id Uuid
              projection WelcomeRows => WelcomeRow
                from Welcomed key id
                  id = id
                  text = text
        """;

    public static ArtifactRenderPlan Plan(string source, bool strict = false, string? slice = null)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("WelcomeModel"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("welcome"), "welcome", "Welcome.play", source);
        var compilation = new SemanticModelCompiler().Compile("WelcomeModel", SemanticDocumentSet.Create([document], catalog));
        if (!compilation.Success)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, compilation.Diagnostics.Select(_ => $"{_.Code} {_.Severity}: {_.Message}")));
        }

        var model = compilation.Value!.Model;
        var options = new CratisRenderingOptions("WelcomeApp", "Welcome") { Strict = strict };
        var scope = slice is null
            ? new ArtifactRenderScope(ArtifactRenderScopeKind.Application, model.Application.Id)
            : new ArtifactRenderScope(ArtifactRenderScopeKind.Slice, model.Application.Modules.Single().Features.Single().Slices.Single(_ => _.Name == slice).Id);
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile(model.Application.Name, options), scope)
        {
            IdentityCatalog = compilation.Value.Documents.IdentityCatalog,
            SemanticIdentities = compilation.Value.Documents.IdentityCatalog.Semantics
        };
        return new CratisArtifactRenderPlanner().Plan(request);
    }
}
