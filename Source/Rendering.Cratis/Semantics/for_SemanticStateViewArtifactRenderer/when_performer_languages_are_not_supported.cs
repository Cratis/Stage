// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateViewArtifactRenderer;

public class when_performer_languages_are_not_supported
{
    internal const string Independent = """

        module Independent
          feature Pings
            slice StateChange Ping
              command Ping
                id Uuid identifier
                produces Pinged
                  for id
              event Pinged
        """;
    [Theory]
    [InlineData("java", false)]
    [InlineData("html", false)]
    [InlineData("sql", false)]
    [InlineData("typescript", false)]
    public async Task should_stub_an_inline_performer_without_losing_other_slices(string language, bool live)
    {
        var source = Source(live)
            .Replace("```csharp", $"```{language}", StringComparison.Ordinal) + Independent;
        var loaded = await when_rendering_a_pure_reducer.Load(source);
        AssertLanguageGap(when_rendering_a_pure_reducer.Plan(loaded), loaded, language);
    }

    public enum BodyEnvelope
    {
        InlineCSharp,
        CSharpFile,
        MixedCSharpJavaFile,
        InlineJava,
        UppercaseCSharpFile,
        ExtensionlessFile,
        JavaFile,
        TypeScriptFile
    }

    [Theory]
    [InlineData(BodyEnvelope.JavaFile)]
    [InlineData(BodyEnvelope.TypeScriptFile)]
    public async Task should_stub_a_file_performer_without_losing_other_slices(BodyEnvelope envelope)
    {
        var extension = envelope == BodyEnvelope.JavaFile ? "java" : "ts";
        var source = Source();
        var loaded = await LoadFile(source, extension);
        AssertLanguageGap(when_rendering_a_pure_reducer.Plan(loaded), loaded, extension);
    }

    [Theory]
    [InlineData(BodyEnvelope.InlineCSharp)]
    [InlineData(BodyEnvelope.CSharpFile)]
    [InlineData(BodyEnvelope.MixedCSharpJavaFile)]
    [InlineData(BodyEnvelope.InlineJava)]
    [InlineData(BodyEnvelope.UppercaseCSharpFile)]
    [InlineData(BodyEnvelope.ExtensionlessFile)]
    public async Task should_keep_the_existing_csharp_file_fallback(BodyEnvelope envelope)
    {
        var (language, file, supported) = envelope switch
        {
            BodyEnvelope.InlineCSharp => ("csharp", null, true),
            BodyEnvelope.CSharpFile => (null, "Body.cs", true),
            BodyEnvelope.MixedCSharpJavaFile => ("csharp", "Body.java", false),
            BodyEnvelope.InlineJava => ("java", null, false),
            BodyEnvelope.UppercaseCSharpFile => (null, "Body.CS", false),
            _ => (null, "Body", false)
        };
        var loaded = await when_rendering_a_pure_reducer.Load(Source());
        var requirement = loaded.ImplementationRequirements.Single() with { Language = language, File = file };
        Assert.Equal(supported, ImplementationLanguage.IsCSharp(requirement));
        if (envelope == BodyEnvelope.ExtensionlessFile) Assert.Contains("file 'Body'", ImplementationLanguage.Reason(requirement), StringComparison.Ordinal);
    }

    static string Source(bool live = false) => $"""
        module People
          feature Directory
            slice StateView People
              readmodel PersonSummary
                personId Uuid
                name String
              query FindPeople => {(live ? "observable " : string.Empty)}PersonSummary[]
                filter name String
                performer
                  ```csharp
                  return readModels;
                  ```
        """;

    internal static async Task<LoadedSemanticModel> LoadFile(string source, string extension)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !IsRepositoryRoot(root)) root = root.Parent;
        Assert.NotNull(root);
        var folder = Path.Combine(root.FullName, ".ai-work", "stage-771", "fixtures", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var start = source.IndexOf("```csharp", StringComparison.Ordinal);
            var end = source.IndexOf("```", start + 3, StringComparison.Ordinal) + 3;
            source = source[..start] + $"file Body.{extension}" + source[end..];
            await File.WriteAllTextAsync(Path.Combine(folder, "Model.play"), source + Independent);
            await File.WriteAllTextAsync(Path.Combine(folder, $"Body.{extension}"), "unsupported body");
            return await SemanticModelLoader.LoadFromPathAsync(folder, null, "Projects");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    static bool IsRepositoryRoot(DirectoryInfo root) => File.Exists(Path.Combine(root.FullName, ".git")) || Directory.Exists(Path.Combine(root.FullName, ".git"));

    internal static void AssertLanguageGap(ArtifactRenderPlan plan, LoadedSemanticModel loaded, string language, IReadOnlyList<string>? baselineWarnings = null)
    {
        var requirement = Assert.Single(loaded.ImplementationRequirements);
        var diagnostic = Assert.Single(plan.Diagnostics.Where(diagnostic => diagnostic.Code == ArtifactRenderDegradationCodes.InlineMismatch));
        Assert.Equal(ArtifactRenderDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(requirement.Source.SemanticId, diagnostic.Artifact);
        Assert.Contains($"language '{language}'", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains(ImplementationLanguage.Remedy, diagnostic.Message, StringComparison.Ordinal);
        var degradation = Assert.Single(plan.Degradations, degradation => degradation.Diagnostic == diagnostic);
        Assert.Equal(ImplementationLanguage.Remedy, degradation.Remedy);
        Assert.Equal(ArtifactRenderOutcome.Degraded, plan.Outcome);
        Assert.False(plan.Strict);
        Assert.DoesNotContain(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-IMPL-006" || diagnostic.Code == "STAGE-IMPL-007");
        Assert.Empty(plan.ImplementationGaps);
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        Assert.DoesNotContain(plan.Artifacts, artifact => artifact.RelativePath == "STAGE-NOT-RUNNABLE.md");
        Assert.Contains(plan.Artifacts, artifact => artifact.RelativePath.EndsWith("/Ping.cs", StringComparison.Ordinal));
        Assert.Contains(plan.Artifacts, artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) &&
            Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Contains("NotSupportedException(\"STAGE-DEGRADE-003\")", StringComparison.Ordinal));
        var files = opaque_policy_model.Files(plan);
        Assert.Empty(RenderedOutput.Errors(files));
        Assert.Equal(baselineWarnings ?? [], RenderedOutput.Warnings(files));
    }
}
#endif
