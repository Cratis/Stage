// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_RenderCompilation.given;

public abstract class a_captured_render : a_generated_application
{
    protected compilation_result _result = null!;
    protected abstract string FixtureName { get; }
    protected override bool DeferPlan => true;
    protected render_capture Capture => render_compilation.Captures[FixtureName];

    void Establish() => Capture.AssertAdmitted();

    protected async Task<compilation_result> CompileCaptured(ArtifactRenderPlan plan)
    {
        var task = render_compilation.Compilations.Get(plan.Artifacts, () => CompileExact(plan));
        if (ReferenceEquals(plan, Capture.Plan)) render_compilation.Results[FixtureName] = task;
        var result = await task;
        Console.WriteLine($"Fixture: {FixtureName}\n{result.Details}\n" +
            $"Compile invocations: {render_compilation.Compilations.Invocations}; requests: {render_compilation.Compilations.Requests}");

        return result;
    }

    async Task<compilation_result> CompileExact(ArtifactRenderPlan plan)
    {
        var layout = compilation_layout.Inspect(plan);
        layout.AssertComplete();
        RenderApplication(plan);
        var files = GeneratedFiles("*").Select(path => path.Replace('\\', '/')).Order(StringComparer.Ordinal);
        var exact = files.SequenceEqual(plan.Artifacts.Select(artifact => artifact.RelativePath).Order(StringComparer.Ordinal)) &&
            plan.Artifacts.All(artifact => ReadGeneratedBytes(artifact.RelativePath).AsSpan().SequenceEqual(artifact.Bytes.AsSpan()));
        Assert.True(exact, "Materialized file paths and bytes must equal the entire captured artifact set before compilation.");
        var execution = await RunExecution("captured-build.log", TimeSpan.FromSeconds(120), "build", layout.Solution, "--configuration", "Debug", "--nologo");

        var result = new compilation_result(layout, exact, execution);
        await File.AppendAllTextAsync(execution.LogPath, $"\nFixture: {FixtureName}\n{result.Summary}\n" +
            $"Compile invocations: {render_compilation.Compilations.Invocations}; requests: {render_compilation.Compilations.Requests}\n");

        return result;
    }

    protected void AssertCompilation()
    {
        Capture.AssertAdmitted();
        _result.Layout.AssertComplete();
        Assert.True(_result.Success, $"{FixtureName}\n{_result.Details}\n" +
            $"Compile invocations: {render_compilation.Compilations.Invocations}; requests: {render_compilation.Compilations.Requests}");
    }
}
#endif
