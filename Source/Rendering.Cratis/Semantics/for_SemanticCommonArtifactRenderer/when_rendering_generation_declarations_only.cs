// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCommonArtifactRenderer.given;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCommonArtifactRenderer;

public class when_rendering_generation_declarations_only : Specification
{
    SemanticApplicationContext _context = null!;
    IReadOnlyList<RenderedFile> _files = null!;

    void Establish() => _context = declared_generation.Context();
    void Because() => _files = [.. _context.Application.Concepts
        .Select(concept => SemanticCommonArtifactRenderer.Render(concept, _context, declarationOnly: true))];

    [Fact]
    void should_declare_a_factory_for_each_supported_explicit_strategy()
    {
        foreach (var concept in _context.Application.Concepts.Where(concept => concept.Generation is not null))
        {
            var source = _files.Single(file => file.RelativePath == $"Common/{concept.Name}.cs").Content;
            var factory = Assert.Single(CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>(), method => method.Identifier.ValueText == "New");
            Assert.Contains(factory.Modifiers, modifier => modifier.ValueText == "static");
            var stub = Assert.IsType<ThrowExpressionSyntax>(factory.ExpressionBody?.Expression);
            var exception = Assert.IsType<ObjectCreationExpressionSyntax>(stub.Expression);
            Assert.Equal("NotImplementedException", exception.Type.GetLastToken().ValueText);
        }
    }

    [Fact] void should_not_execute_or_retain_strategy_implementations() => Assert.All(_files, file =>
    {
        Assert.DoesNotContain("GenerateValue.", file.Content);
        Assert.DoesNotContain("return \"Generated\";", file.Content);
    });

    [Fact] void should_compile_the_declarations() => RenderedOutput.Errors(_files).ShouldBeEmpty();

    [Fact]
    void should_not_invent_a_factory_for_an_unsupported_strategy()
    {
        var concept = _context.Application.Concepts.Single(concept => concept.Name == "StringUuid") with { Generation = new SemanticGeneration("future") };
        SemanticGenerationShapes.Supported(concept).ShouldBeFalse();
        SemanticCommonArtifactRenderer.IsGeneratable(concept, _context).ShouldBeFalse();
    }

    [Fact]
    void should_refuse_an_unsupported_generation_envelope()
    {
        var request = _context.Request with
        {
            ImplementationRequirements = [.. _context.Request.ImplementationRequirements.Select(requirement => requirement with { ContextVersion = 99 })]
        };
        var plan = new CratisArtifactRenderPlanner().Plan(request);
        plan.Success.ShouldBeFalse();
        plan.Diagnostics.ShouldNotBeEmpty();
    }
}
