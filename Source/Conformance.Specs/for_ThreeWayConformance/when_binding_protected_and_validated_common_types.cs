// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_binding_protected_and_validated_common_types
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_preserve_attribute_collection_enum_and_validator_bindings(bool compliance)
    {
        var model = invoice_model.Compile(compliance ? a_concept_compliance_model.Source + "\n" + a_concept_compliance_model.ContainerTypes : a_validated_query_key_model.Source);
        var options = new CratisRenderingOptions("ProofApp", "ProofApp");
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile("ProofApp", options), new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, options);
        var qualified = model.Application.Concepts.Select(concept => SemanticCommonArtifactRenderer.Render(concept, context, qualified: true))
            .Concat(model.Application.Types.Select(type => SemanticCommonArtifactRenderer.Render(type, context, qualified: true))).ToArray();
        var rendered = model.Application.Concepts.Select(concept => SemanticCommonArtifactRenderer.Render(concept, context))
            .Concat(model.Application.Types.Select(type => SemanticCommonArtifactRenderer.Render(type, context))).ToArray();
        Assert.NotEmpty(qualified);
        Assert.Empty(QualifiedOutputProof.VerifyRendered(qualified, rendered).Failures);
    }
}
#endif
