// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_a_scoped_application_with_and_without_scaffold_globals
{
    const string Source = """
        concept ProjectName : String
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId String identifier
                name ProjectName
                produces ProjectRegistered
                  for projectId
                  name = name
              event ProjectRegistered
                name ProjectName
            slice StateView ProjectLookup
              readmodel ProjectSummary
                key String
                name ProjectName
              query ProjectById => ProjectSummary?
                by key String
              query AllProjects => observable ProjectSummary[]
              projection ProjectSummaryProjection => ProjectSummary
                from ProjectRegistered key $eventSourceId
                  name = name
              specification ListingProjects
                given ProjectRegistered
                  for "project-1"
                  name = "hello"
                then query AllProjects
                  result
                    key = "project-1"
                    name = "hello"
        """;

    [Theory]
    [InlineData(ArtifactRenderScopeKind.Module)]
    [InlineData(ArtifactRenderScopeKind.Feature)]
    public void should_compile_and_bind_alike_with_and_without_the_scaffold_file(ArtifactRenderScopeKind kind)
    {
        var model = invoice_model.Compile(Source);
        var module = model.Application.Modules.Single();
        var scope = new ArtifactRenderScope(kind, kind == ArtifactRenderScopeKind.Module ? module.Id : module.Features.Single().Id);
        var rendered = invoice_model.Plan(model, scope);
        ArtifactRenderPlan qualified;
        using (QualifiedSpelling.Begin()) qualified = invoice_model.Plan(model, scope);

        Assert.DoesNotContain(rendered.Artifacts, artifact => artifact.RelativePath.EndsWith("GlobalUsings.cs", StringComparison.Ordinal));
        foreach (var legacy in new[] { true, false })
        {
            var errors = QualifiedOutputProof.CompilationErrors(Files(rendered), legacy);
            Assert.True(errors.Count == 0, string.Join('\n', errors));
            var proof = QualifiedOutputProof.VerifyRendered(Files(qualified), Files(rendered), legacyImports: legacy);
            Assert.True(proof.Failures.Count == 0, string.Join('\n', proof.Failures));
            Assert.True(proof.ShortSpellings > 0, "The module spells no type by a short name.");
        }
    }

    [Fact]
    public void should_place_every_using_before_the_namespace()
    {
        var model = invoice_model.Compile(Source);
        var scope = new ArtifactRenderScope(ArtifactRenderScopeKind.Module, model.Application.Modules.Single().Id);
        foreach (var file in Files(invoice_model.Plan(model, scope)))
        {
            var lines = file.Content.Split('\n');
            var namespaceLine = Array.FindIndex(lines, line => line.StartsWith("namespace ", StringComparison.Ordinal));
            var lastUsing = Array.FindLastIndex(lines, line => line.StartsWith("using ", StringComparison.Ordinal) && !line.Contains('('));
            Assert.True(namespaceLine < 0 || lastUsing < namespaceLine, $"{file.RelativePath} has a using after its namespace.");
        }
    }

    static RenderedFile[] Files(ArtifactRenderPlan plan) => [.. plan.Artifacts
        .Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal))
        .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())))];
}
#endif
