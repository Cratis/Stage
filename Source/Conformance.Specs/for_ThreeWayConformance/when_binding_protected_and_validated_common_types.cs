// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_binding_protected_and_validated_common_types
{
    [Theory]
    [InlineData(false, null)]
    [InlineData(true, null)]
    [InlineData(true, "namespace ProofApp.Cratis.Concepts { public record ConceptAs<T>(T Value); }")]
    [InlineData(true, "namespace ProofApp.System.Collections.Generic { public interface IReadOnlyList<T>; }")]
    public void should_preserve_attribute_collection_enum_and_validator_bindings(bool compliance, string? prefixShadow)
    {
        const string pattern = """
            concept InvoiceLabel : String
              validate
                matches "^X" severity warning
            """;
        var source = compliance ? a_concept_compliance_model.Source + "\n" + a_concept_compliance_model.ContainerTypes : a_validated_query_key_model.Source + "\n" + pattern;
        var model = invoice_model.Compile(source);
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join('\n', execution.Issues));
        var options = new CratisRenderingOptions("ProofApp", "ProofApp");
        var request = new ArtifactRenderRequest(model, execution.Plan!, CratisRendering.CreateProfile(model.Application.Name, options), new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var plan = new CratisArtifactRenderPlanner().Plan(request);
        Assert.True(plan.Success, string.Join('\n', plan.Diagnostics));
        var context = new SemanticApplicationContext(request, options);
        var qualified = model.Application.Concepts.Select(concept => SemanticCommonArtifactRenderer.Render(concept, context, qualified: true))
            .Concat(model.Application.Types.Select(type => SemanticCommonArtifactRenderer.Render(type, context, qualified: true))).ToArray();
        var rendered = plan.Artifacts.Where(artifact => artifact.RelativePath.StartsWith("Common/", StringComparison.Ordinal))
            .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan()))).ToArray();
        Assert.NotEmpty(qualified);
        if (prefixShadow is not null)
        {
            var hostile = new RenderedFile("Customizations/Prefix.cs", prefixShadow);
            qualified = [.. qualified, hostile];
            rendered = [.. rendered, hostile];
        }

        Assert.Empty(QualifiedOutputProof.CompilationErrors(qualified));
        Assert.Empty(QualifiedOutputProof.CompilationErrors(rendered));
        Assert.Empty(QualifiedOutputProof.VerifyRendered(qualified, rendered).Failures);
    }
}
#endif
