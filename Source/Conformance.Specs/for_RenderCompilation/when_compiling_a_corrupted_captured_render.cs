// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Stage.Conformance.Specs.for_RenderCompilation.given;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_RenderCompilation;

public class when_compiling_a_corrupted_captured_render : a_captured_render
{
    protected override string FixtureName => "AcmeAdmissionsCorpus.V29";
    ArtifactRenderPlan _corrupted = null!;
    string _path = null!;

    void Establish()
    {
        var original = Capture.Plan!;

        // Program.cs belongs to the application, not a downstream specification project.
        var artifact = original.Artifacts.Single(artifact => artifact.RelativePath == "Program.cs");
        _path = artifact.RelativePath;
        var replacement = PlannedArtifact.CreateText(
            _path,
            Encoding.UTF8.GetString(artifact.Bytes.AsSpan()) + "\n#error STAGE824_CORRUPTION\n",
            artifact.Sources);
        _corrupted = ArtifactRenderPlan.Create(
            Capture.Request!,
            [.. original.Artifacts.Select(candidate => candidate.RelativePath == _path ? replacement : candidate)],
            original.Diagnostics,
            original.ImplementationGaps,
            original.Refusals,
            original.RequiredRemovals);
    }

    async Task Because() => _result = await CompileCaptured(_corrupted);

    [Fact]
    void should_observe_the_injected_compiler_error_not_just_render_admission()
    {
        Capture.AssertAdmitted();
        Assert.True(_corrupted.Success);
        Assert.Equal(1, Capture.Plan!.Artifacts.Zip(_corrupted.Artifacts).Count(pair => !pair.First.Bytes.SequenceEqual(pair.Second.Bytes)));
        Assert.Equal(Capture.Plan.Artifacts.Select(artifact => artifact.RelativePath), _corrupted.Artifacts.Select(artifact => artifact.RelativePath));
        Assert.True(_result.ExactMaterialization, _result.Details);
        _result.Layout.AssertComplete();
        Assert.True(_result.HasCorruptionDiagnostic(_path), _result.Details);
    }
}
#endif
