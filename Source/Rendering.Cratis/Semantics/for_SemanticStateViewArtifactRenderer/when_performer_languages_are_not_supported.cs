// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateViewArtifactRenderer;

public class when_performer_languages_are_not_supported
{
    public enum Language
    {
        Java,
        Html,
        Sql,
        TypeScript,
        Ts
    }

    [Theory]
    [InlineData(Language.Html, false)]
    [InlineData(Language.Sql, false)]
    [InlineData(Language.TypeScript, false)]
    [InlineData(Language.Html, true)]
    public async Task should_stub_an_inline_performer_without_losing_other_slices(Language language, bool live)
    {
        var source = when_executing_performed_queries.Source("return readModels;", live: live)
            .Replace("```csharp", $"```{Name(language)}", StringComparison.Ordinal) + a_render_with_an_invalid_typed_context.Independent;
        var loaded = await when_rendering_a_pure_reducer.Load(source);
        AssertLanguageGap(when_executing_performed_queries.Request(loaded), language);
    }

    [Theory]
    [InlineData(Language.Java)]
    [InlineData(Language.Ts)]
    public async Task should_stub_a_file_performer_without_losing_other_slices(Language language)
    {
        var loaded = await LoadFile(Name(language));
        AssertLanguageGap(when_executing_performed_queries.Request(loaded), language);
    }

    static async Task<LoadedSemanticModel> LoadFile(string extension)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !IsRepository(root)) root = root.Parent;
        Assert.NotNull(root);
        var folder = Path.Combine(root.FullName, ".ai-work", "stage-performer", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(folder, "Queries"));
        try
        {
            var source = when_executing_performed_queries.Source("return readModels;")
                .Replace("```csharp\n          return readModels;\n          ```", $"file Queries/FindPeople.{extension}", StringComparison.Ordinal) + a_render_with_an_invalid_typed_context.Independent;
            await File.WriteAllTextAsync(Path.Combine(folder, "People.play"), source);
            await File.WriteAllTextAsync(Path.Combine(folder, "Queries", $"FindPeople.{extension}"), "return readModels;");
            return await SemanticModelLoader.LoadFromPathAsync(folder, null, "Projects");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    static bool IsRepository(DirectoryInfo root) => File.Exists(Path.Combine(root.FullName, ".git")) || Directory.Exists(Path.Combine(root.FullName, ".git"));

    static string Name(Language language) => language.ToString().ToLowerInvariant();

    static void AssertLanguageGap(ArtifactRenderRequest request, Language language)
    {
        var plan = new CratisArtifactRenderPlanner().Plan(request);
        var requirement = Assert.Single(request.ImplementationRequirements);
        var diagnostic = Assert.Single(plan.Diagnostics.Where(diagnostic => diagnostic.Code == "STAGE-ESM-019"));
        Assert.Equal(ArtifactRenderDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(requirement.Source.SemanticId, diagnostic.Artifact);
        Assert.Contains($"language '{Name(language)}'", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("Cratis target only realizes C# bodies", diagnostic.Message, StringComparison.Ordinal);
        var gap = Assert.Single(plan.ImplementationGaps);
        Assert.Equal(ArtifactImplementationGapKind.Language, gap.Kind);
        Assert.Equal(requirement.Source.SemanticId.ToString(), gap.Owner);
        Assert.Equal(requirement.RequirementId, gap.RequirementId);
        var stub = Assert.Single(gap.Stubs);
        var artifact = plan.Artifacts.Single(artifact => artifact.RelativePath == stub.RelativePath);
        Assert.Contains("throw new global::System.NotImplementedException", System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan()), StringComparison.Ordinal);
        Assert.Equal(ArtifactRenderOutcome.Partial, plan.Outcome);
        Assert.Contains(plan.Artifacts, artifact => artifact.RelativePath == "STAGE-NOT-RUNNABLE.md");
        Assert.Contains(plan.Artifacts, artifact => artifact.RelativePath.EndsWith("/Ping.cs", StringComparison.Ordinal));
        var files = opaque_policy_model.Files(plan);
        Assert.Empty(RenderedOutput.Errors(files));
        Assert.Empty(RenderedOutput.Warnings(files));
        Assert.DoesNotContain(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-IMPL-006");
    }
}
#endif
