// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues.given;

internal static class a_code_provided_model
{
    internal static string Source(string body, string type = "String") =>
        "provide region " + type + "\n  ```csharp\n" +
        string.Join('\n', body.Split('\n').Select(line => "  " + line)) + "\n  ```\n" + a_fleet_model.Source;

    internal static ArtifactRenderRequest Request(string body, string type = "String") => RequestFromSource(Source(body, type));

    internal static ArtifactRenderRequest RequestFromSource(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Fleet"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("fleet"), "fleet", "Fleet.play", source);
        var compiled = new SemanticModelCompiler().Compile("Fleet", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compiled.Success, string.Join("; ", compiled.Diagnostics));
        var compilation = compiled.Value!;
        var model = compilation.Model;
        return new(
            model,
            SemanticExecutionPlan.Compile(model, []).Plan!,
            CratisRendering.WithRealization(CratisRendering.CreateProfile("Fleet", new("Fleet", "Fleet")), compilation),
            new(ArtifactRenderScopeKind.Application, model.Application.Id))
        {
            ImplementationRequirements = compiled.ImplementationRequirements,
            ImplementationContents = SemanticImplementationBodies.Resolve(compilation.Documents, compiled.ImplementationRequirements),
            TypedContextDescriptors = compiled.TypedContextDescriptors
        };
    }

    internal static ArtifactRenderPlan Plan(string body, string type = "String") => Plan(Request(body, type));
    internal static ArtifactRenderPlan Plan(ArtifactRenderRequest request) => new CratisArtifactRenderPlanner().Plan(request);
    internal static void AssertSuccess(ArtifactRenderPlan plan) => Assert.True(plan.Success, string.Join("; ", plan.Diagnostics));
}
#endif
