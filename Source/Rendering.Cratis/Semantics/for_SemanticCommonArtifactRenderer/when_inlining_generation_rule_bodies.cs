// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCommonArtifactRenderer.given;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCommonArtifactRenderer;

public class when_inlining_generation_rule_bodies
{
    [Theory]
    [InlineData("return context.Tenant.ToString();", true)]
    [InlineData("return \"tenant\";", true)]
    [InlineData("return $\"tenant:{context.Tenant}\";", true)]
    [InlineData("return \"tenant:\" + context.Tenant.ToString();", true)]
    [InlineData("return string.Concat(\"tenant:\", context.Tenant);", true)]
    [InlineData("return nameof(context.Tenant);", true)]
    [InlineData("if (context.Tenant.ToString().Length == 0) return \"empty\";\nreturn context.Tenant.ToString();", true)]
    [InlineData("var value = context.Tenant.ToString();\nreturn value;", false)]
    [InlineData("return new('x', 3);", false)]
    [InlineData("return default(string)!;", false)]
    [InlineData("return new ConceptAs<string>(context.Tenant);", false)]
    [InlineData("var value = new ConceptAs<string>(context.Tenant);\nreturn value;", false)]
    [InlineData("return (ConceptAs<string>)new ConceptAs<string>(context.Tenant);", false)]
    [InlineData("return new ConceptAs<ConceptAs<string>>(new ConceptAs<string>(context.Tenant)).Value;", false)]
    void should_preserve_verified_bodies_and_use_the_helper_unless_every_return_is_provably_string(string body, bool inline)
    {
        var context = declared_generation.Context(body);
        var concept = context.Application.Concepts.Single(concept => concept.Name == "TenantOrderId");
        var plan = new CratisArtifactRenderPlanner().Plan(context.Request);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var source = Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath == "Common/TenantOrderId.cs").Bytes.AsSpan());
        var factory = Assert.Single(CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>(), method => method.Identifier.ValueText == "New");
        var helper = SemanticGenerationRendering.BodyMethod(concept, context);
        if (inline)
        {
            Assert.NotNull(factory.Body);
            Assert.Null(factory.ExpressionBody);
            Assert.DoesNotContain(helper + "(context)", source);
        }
        else
        {
            Assert.NotNull(factory.ExpressionBody);
            Assert.Contains(helper + "(context)", source);
        }

        Assert.All(body.Split('\n'), line => Assert.Contains(line, source));
        var files = plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs" && !artifact.RelativePath.StartsWith("Frontend/", StringComparison.Ordinal))
            .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
        RenderedOutput.Errors(files).ShouldBeEmpty();
    }
}
