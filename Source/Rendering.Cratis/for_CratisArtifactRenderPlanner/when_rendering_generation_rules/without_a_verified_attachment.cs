// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_generation_rules;

public class without_a_verified_attachment
{
    [Theory]
    [InlineData("missing")]
    [InlineData("unresolved")]
    [InlineData("tampered")]
    [InlineData("omitted")]
    public void should_refuse_an_unverified_generation_body(string kind)
    {
        var loaded = generation_rule_model.Load();
        var requirements = loaded.ImplementationRequirements;
        var contents = loaded.ImplementationContents;
        if (kind == "missing") contents = [];
        if (kind == "omitted")
        {
            requirements = [];
            contents = [];
        }
        if (kind == "tampered") contents = contents.SetItem(requirements.Single().RequirementId, "return \"changed\";");
        if (kind == "unresolved") requirements = [requirements.Single() with { AttachmentResolution = SemanticAttachmentResolution.UnresolvedFile }];
        var plan = CratisRendering.Plan(
            loaded.Model,
            loaded.Plan,
            new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id),
            new("InvoiceApp", "Invoices"),
            requirements,
            contents,
            loaded.AttachmentDiagnostics,
            kind == "omitted" ? [] : loaded.TypedContextDescriptors);
        Assert.Empty(plan.Artifacts);
        var expectedCode = kind == "tampered" ? "STAGE-ESM-056" : "STAGE-ESM-020";
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == expectedCode);
    }

    [Fact]
    public void should_require_the_compilation_typed_context()
    {
        var loaded = generation_rule_model.Load();
        var plan = CratisRendering.Plan(
            loaded.Model,
            loaded.Plan,
            new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id),
            new("InvoiceApp", "Invoices"),
            loaded.ImplementationRequirements,
            loaded.ImplementationContents,
            loaded.AttachmentDiagnostics,
            []);
        Assert.Empty(plan.Artifacts);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-021");
    }
}
#endif
