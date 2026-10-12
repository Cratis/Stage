// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_concept_compliance_scopes
{
    [Theory]
    [InlineData(null, "[global::Cratis.Chronicle.ProtectedValues.EncryptedAttribute]")]
    [InlineData("subject", "[global::Cratis.Chronicle.ProtectedValues.EncryptedAttribute]")]
    [InlineData("namespace", "[global::Cratis.Chronicle.ProtectedValues.EncryptedAttribute(global::Cratis.Chronicle.ProtectedValues.EncryptionScope.Namespace)]")]
    [InlineData("global", "[global::Cratis.Chronicle.ProtectedValues.EncryptedAttribute(global::Cratis.Chronicle.ProtectedValues.EncryptionScope.Global)]")]
    public void should_preserve_the_secret_scope_without_details(string? scope, string expected)
    {
        var builder = new CSharpCodeBuilder();
        ConceptComplianceRendering.Render(builder, new SemanticConceptCompliance(null, new(scope)));
        var source = builder.ToString();
        source.ShouldContain(expected);
        source.ShouldContain("[global::Cratis.Arc.Chronicle.Commands.NotAuditedAttribute]");
        source.ShouldNotContain("ComplianceDetails");
    }
}
