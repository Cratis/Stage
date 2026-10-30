// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_bodies_without_a_csharp_body;

public class with_a_java_performer
{
    const string Source = """
        module Billing
          feature Invoices
            slice StateView Figures
              readmodel Figure
                figureId String
                total Int
              query AllFigures => Figure[]
                performer
                  file Queries/AllFigures.cs
        """;

    [Theory]
    [InlineData(ImplementationGapMode.Refuse, false)]
    [InlineData(ImplementationGapMode.Refuse, true)]
    [InlineData(ImplementationGapMode.Stub, false)]
    [InlineData(ImplementationGapMode.Stub, true)]
    public void should_refuse_instead_of_rendering_a_performer_stub(ImplementationGapMode mode, bool strict)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("billing"), "billing", "Billing.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Billing", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;
        const string body = "return figures;";
        var requirement = Assert.Single(compilation.ImplementationRequirements) with
        {
            Language = "java",
            File = null,
            AttachmentResolution = SemanticAttachmentResolution.Resolved,
            ContentHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(body)))
        };
        var plan = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Billing", "Billing") { ImplementationGaps = mode, Strict = strict }, [requirement], ImmutableDictionary<string, string>.Empty.Add(requirement.RequirementId, body));

        Assert.Equal(ArtifactRenderOutcome.Refused, plan.Outcome);
        var diagnostic = Assert.Single(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-IMPL-LANGUAGE");
        Assert.Equal(ArtifactRenderDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(requirement.Source.SemanticId, diagnostic.Artifact);
        Assert.Equal($"Query performer for 'AllFigures' (requirement {requirement.RequirementId}) has implementation bodies in java but none in csharp. Add a csharp body to render it with the Cratis target.", diagnostic.Message);
        Assert.Empty(plan.ImplementationGaps);
        Assert.Empty(plan.Artifacts);
        Assert.DoesNotContain(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-IMPL-006" || diagnostic.Code == "STAGE-ESM-019" || diagnostic.Code == "STAGE-INLINE-INTEGRITY");
    }
}
