// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_a_part_fails_after_yielding_an_artifact : given.a_register_project_render_request
{
    [Theory]
    [InlineData(true, "STAGE-ESM-021")]
    [InlineData(false, "STAGE-ESM-012")]
    public void should_discard_only_the_failed_producer(bool invalidContext, string code)
    {
        var diagnostics = new List<ArtifactRenderDiagnostic>();
        var parts = new RenderingPartAdmission(new(_request, new("Projects", "Projects")), diagnostics);
        var files = parts.Render(_projectLookup.Id, () => FailingProducer(invalidContext));
        files.ShouldBeEmpty();
        diagnostics.Count.ShouldEqual(1);
        diagnostics[0].Artifact.ShouldEqual(_projectLookup.Id);
        diagnostics[0].Code.ShouldEqual(code);
        diagnostics[0].Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Warning);
        parts.Render(_registerProject.Id, () => [new("Independent.cs", "public class Independent { }")]).Count.ShouldEqual(1);
        RenderingGapReports.FromDiagnostics(diagnostics).Single().Kind.ShouldEqual(ArtifactImplementationGapKind.Rendering);
    }

    static IEnumerable<RenderedFile> FailingProducer(bool invalidContext)
    {
        yield return new("Incomplete.cs", "public class Incomplete { }");
        if (invalidContext) throw new InvalidTypedContext("Invalid typed context.");
        throw UnsupportedSemanticRendering.For("Primitive", 999);
    }
}
