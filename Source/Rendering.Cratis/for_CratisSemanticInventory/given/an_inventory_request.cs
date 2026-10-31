// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.for_CratisSemanticInventory.given;

public class an_inventory_request : Specification
{
    protected ArtifactRenderRequest _request = null!;
    protected CratisRenderingOptions _options = new("Projects", "Projects");
    protected SemanticSlice _registration = null!;
    protected SemanticSlice _renaming = null!;
    protected ArtifactRenderPlan _plan = null!;
    protected ArtifactRenderInventory _inventory = null!;
    protected Dictionary<SemanticId, ImmutableArray<SemanticId>> _causes = [];

    void Establish()
    {
        const string source = """
            module Projects
              feature Management
                slice StateChange RegisterProject
                  command RegisterProject
                    projectId Uuid identifier
                    name String
                    produces ProjectRegistered
                      for projectId
                      name = name
                  event ProjectRegistered
                    name String
                  specification RegisteringProject
                    when RegisterProject
                      projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                      name = "Screenplay"
                    then ProjectRegistered
                      name = "Screenplay"
                slice StateChange RenameProject
                  command RenameProject
                    projectId Uuid identifier
                    name String
                    produces ProjectRenamed
                      for projectId
                      name = name
                  event ProjectRenamed
                    name String
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("projects"), "projects", "Projects.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        compilation.Success.ShouldBeTrue();
        var compiled = compilation.Value!;
        var model = compiled.Model;
        var slices = model.Application.Modules.Single().Features.Single().Slices;
        _registration = slices.Single(slice => slice.Name == "RegisterProject");
        _renaming = slices.Single(slice => slice.Name == "RenameProject");
        _request = new(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile("Projects", _options), new(ArtifactRenderScopeKind.Application, model.Application.Id))
        {
            IdentityCatalog = compiled.Documents.IdentityCatalog,
            SemanticIdentities = compiled.Documents.IdentityCatalog.Semantics
        };
    }

    protected static ArtifactRenderDegradation Evidence(SemanticId owner, string code) =>
        new(new(code, ArtifactRenderDiagnosticSeverity.Warning, "Omitted modeled owner.", owner), "Resolve the unsupported member.");

    protected ArtifactRenderPlan Plan(ImmutableArray<ArtifactRenderDegradation> evidence, ImmutableArray<ArtifactRenderRefusal> refusals) =>
        ArtifactRenderPlan.Create(_request, [], [.. evidence.Select(gap => gap.Diagnostic)], [], refusals).WithAcceptance(evidence);

    protected ArtifactRenderInventory CreateInventory() => CratisSemanticInventory.Create(_request, _plan, _options, _causes);
}
