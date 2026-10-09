// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Emission;
using Cratis.Stage.Rendering.Cratis.for_ConceptRenderer.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_building_compliance_details : a_generated_application
{
    string _warnings = null!;

    protected override ArtifactRenderPlan CreatePlan()
    {
        // Compliance concept metadata is syntax-only (PLAY0268); use the package-owned scaffold
        // with syntax-rendered domain artifacts, as the ordinary secret compile probe does.
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Compliance"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("scaffold"), "scaffold", "Scaffold.play", "module Accounts");
        var compilation = new SemanticModelCompiler().Compile("Compliance", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join(Environment.NewLine, execution.Issues));
        var request = new ArtifactRenderRequest(model, execution.Plan!, CratisRendering.CreateProfile("Compliance", new("Compliance", "Compliance")), new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var scaffold = new CratisArtifactRenderPlanner().Plan(request);
        Assert.True(scaffold.Success, string.Join(Environment.NewLine, scaffold.Diagnostics));
        var syntax = new ScreenplayCompiler().Compile(compliance_concepts.Source);
        Assert.True(syntax.Success, string.Join(Environment.NewLine, syntax.Diagnostics));
        var files = RenderLegacy(syntax.Value!).GetAwaiter().GetResult();
        files.ShouldContain(file => file.Content.Contains("[ComplianceDetails(\"GDPR Art. 9(1) special category: health. GDPR Art. 10 criminal offence data. Clinical evidence.\")]") && file.RelativePath.EndsWith("MedicalNote.cs", StringComparison.Ordinal));
        files.ShouldContain(file => file.Content.Contains("[Encrypted(EncryptionScope.Namespace, \"Shared credential.\")]\n[NotAudited]"));
        return ArtifactRenderPlan.Create(request, [.. scaffold.Artifacts, .. files.Select(file => PlannedArtifact.CreateText(file.RelativePath, file.Content))], scaffold.Diagnostics);
    }

    static async Task<IReadOnlyList<CodeGeneration.RenderedFile>> RenderLegacy(ApplicationSyntax application)
    {
        var codeOutput = new InMemoryCodeOutput();
        var renderer = new CratisRenderer(
            new a_stub_scaffolder(),
            new Dictionary<SliceType, ISliceRenderer> { [SliceType.StateChange] = new StateChangeSliceRenderer() },
            codeOutput) { ComplianceDetails = true };
        await using var output = new StringWriter();
        await using var error = new StringWriter();
        await renderer.Render([application], new DirectoryInfo(Path.Combine(Path.GetTempPath(), "compliance")), output, error);
        Assert.True(error.ToString().Length == 0, error.ToString());
        codeOutput.FailureMarkerWasWritten.ShouldBeFalse();
        return codeOutput.Files;
    }

    async Task Because() => _warnings = BuildWarnings(await Run("compliance-build.log", "build", "Compliance.csproj", "-c", "Debug", "--nologo", "-warnaserror", "-p:CratisProxiesOutputPath="));

    [Fact] void should_build_opted_in_concepts_commands_and_events_with_analyzers_without_warnings() => _warnings.ShouldBeEmpty();
}
#endif
