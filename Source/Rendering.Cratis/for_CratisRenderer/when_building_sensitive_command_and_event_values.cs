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
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_building_sensitive_command_and_event_values : a_generated_application
{
    string _warnings = null!;

    protected override ArtifactRenderPlan CreatePlan()
    {
        // Reuse the package-owned application scaffold, but render every domain artifact through the legacy
        // path: the semantic concept model does not carry these source attributes.
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Secrets"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("scaffold"), "scaffold", "Scaffold.play", "module Accounts");
        var compilation = new SemanticModelCompiler().Compile("Secrets", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join(Environment.NewLine, execution.Issues));
        var profile = CratisRendering.CreateProfile("Secrets", new("Secrets", "Secrets"));
        var request = new ArtifactRenderRequest(model, execution.Plan!, profile, new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var scaffold = new CratisArtifactRenderPlanner().Plan(request);
        scaffold.Success.ShouldBeTrue();

        var syntax = new ScreenplayCompiler().Compile("""
            concept AccountId : Uuid
            concept ApiKey : String @sensitive
            concept PersonalValue : String @pii @sensitive
            module Accounts
              feature Credentials
                slice StateChange SetCredential
                  command SetCredential
                    accountId AccountId identifier
                    apiKey ApiKey
                    personalValue PersonalValue
                    produces CredentialSet
                      apiKey = apiKey
                      personalValue = personalValue
                  event CredentialSet
                    apiKey ApiKey
                    personalValue PersonalValue
            """);
        Assert.True(syntax.Success, string.Join(Environment.NewLine, syntax.Diagnostics));
        var files = RenderLegacy(syntax.Value!).GetAwaiter().GetResult();
        files.ShouldContain(file => file.RelativePath.EndsWith("ApiKey.cs", StringComparison.Ordinal) && file.Content.Contains("[Encrypted]\n[NotAudited]", StringComparison.Ordinal));
        files.ShouldContain(file => file.Content.Contains("ApiKey ApiKey", StringComparison.Ordinal) && file.Content.Contains("public record SetCredential", StringComparison.Ordinal));
        files.ShouldContain(file => file.Content.Contains("ApiKey ApiKey", StringComparison.Ordinal) && file.Content.Contains("public record CredentialSet", StringComparison.Ordinal));
        return ArtifactRenderPlan.Create(request, [.. scaffold.Artifacts, .. files.Select(file => PlannedArtifact.CreateText(file.RelativePath, file.Content))], scaffold.Diagnostics);
    }

    static async Task<IReadOnlyList<CodeGeneration.RenderedFile>> RenderLegacy(ApplicationSyntax application)
    {
        var codeOutput = new InMemoryCodeOutput();
        var renderer = new CratisRenderer(
            new a_stub_scaffolder(),
            new Dictionary<SliceType, ISliceRenderer> { [SliceType.StateChange] = new StateChangeSliceRenderer() },
            codeOutput);
        await using var output = new StringWriter();
        await using var error = new StringWriter();
        await renderer.Render([application], new DirectoryInfo(Path.Combine(Path.GetTempPath(), "secrets")), output, error);
        error.ToString().ShouldBeEmpty();
        codeOutput.FailureMarkerWasWritten.ShouldBeFalse();
        return codeOutput.Files;
    }

    async Task Because() => _warnings = BuildWarnings(await Run("sensitive-build.log", "build", "Secrets.csproj", "-c", "Debug", "--nologo", "-warnaserror", "-p:CratisProxiesOutputPath="));

    [Fact] void should_build_command_and_event_concepts_with_analyzers_without_warnings() => _warnings.ShouldBeEmpty();
}
#endif
