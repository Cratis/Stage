// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_binding_rendered_common_short_names : Specification
{
    RenderedFile[] _qualified = null!;
    RenderedFile[] _rendered = null!;
    QualifiedOutputResult _proof = null!;
    SemanticApplicationContext _context = null!;

    void Establish()
    {
        var model = a_generation_model.Model();
        var options = new CratisRenderingOptions("GenerationApp", "GenerationApp");
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile("GenerationApp", options), new(ArtifactRenderScopeKind.Application, model.Application.Id));
        _context = new(request, options);
        _rendered = [.. model.Application.Concepts.Select(concept => SemanticCommonArtifactRenderer.Render(concept, _context))];
        _qualified = [.. model.Application.Concepts.Select(concept => SemanticCommonArtifactRenderer.Render(concept, _context, qualified: true))];
    }

    void Because() => _proof = QualifiedOutputProof.VerifyRendered(_qualified, _rendered);

    [Fact] void should_preserve_every_binding() => Assert.Empty(_proof.Failures);
    [Fact] void should_exercise_more_than_twenty_short_spellings() => _proof.ShortSpellings.ShouldBeGreaterThan(20);
    [Fact] void should_keep_the_qualified_oracle_free_of_in_namespace_imports() => _qualified.All(file => !file.Content.Contains("namespace GenerationApp.Common;\n\nusing", StringComparison.Ordinal)).ShouldBeTrue();

    [Theory]
    [InlineData("public class GenerateValue;")]
    [InlineData("namespace GenerationApp { public class GenerateValue; }")]
    [InlineData("global using GenerateValue = global::System.String;")]
    [InlineData("namespace Cratis.Chronicle.Events { public interface IGeneratableInContext<T>; }")]
    public void should_preserve_bindings_or_refuse_a_hostile_addition(string source)
    {
        var hostile = new RenderedFile("Customizations/Hostile.cs", source);
        var proof = QualifiedOutputProof.VerifyRendered([.. _qualified, hostile], [.. _rendered, hostile], allowShortFailure: true);
        Assert.Empty(proof.Failures);
    }

    [Theory]
    [InlineData(ArtifactRenderScopeKind.Module)]
    [InlineData(ArtifactRenderScopeKind.Slice)]
    public void should_emit_identical_common_bytes_across_plan_scopes(ArtifactRenderScopeKind scope)
    {
        var model = _context.Request.Model;
        var artifact = scope == ArtifactRenderScopeKind.Module ? model.Application.Modules[0].Id : model.Application.Modules[0].Features[0].Slices[1].Id;
        var plan = CratisRendering.Plan(model, _context.Request.ExecutionPlan, new(scope, artifact), new("GenerationApp", "GenerationApp"));
        Assert.True(plan.Success, string.Join('\n', plan.Diagnostics));
        var common = plan.Artifacts.Where(file => file.RelativePath.StartsWith("Common/", StringComparison.Ordinal)).ToArray();
        Assert.NotEmpty(common);
        foreach (var file in common)
        {
            Assert.Equal(_rendered.Single(original => original.RelativePath == file.RelativePath).Content, Encoding.UTF8.GetString(file.Bytes.AsSpan()));
        }
    }
}
#endif
