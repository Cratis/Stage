// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_body_carrying_command_handlers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_reporting_omitted_specification_inventory : a_notify_speaker_render_request
{
    [Fact]
    public void should_report_an_omitted_reaction_specification_with_its_original_evidence()
    {
        var loaded = opaque_policy_model.Load(Source + "\n      specification DenyingGuestMail\n        given caller\n        when SendAcceptanceMail\n          talkId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n        then denied\n");
        var request = new ArtifactRenderRequest(loaded.Model, loaded.Plan, CratisRendering.CreateProfile(loaded.Model.Application.Name, new("InvoiceApp", "Invoices")), new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id))
        {
            IdentityCatalog = loaded.IdentityCatalog,
            SemanticIdentities = loaded.SemanticIdentities
        };
        VerifyReports(new CratisArtifactRenderPlanner().Plan(request), "STAGE-ESM-024");
    }

    [Fact]
    public void should_report_an_omitted_native_command_body_specification_without_completion_credit()
    {
        const string source = handler_model.Source + "\n      specification PlacingOrder\n        when PlaceOrder\n          orderId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          name = \"Alice\"\n        then OrderPlaced\n          name = \"ALICE\"\n";
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("HandlerModel"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("orders"), "orders", "Orders.play", source);
        var compilation = new SemanticModelCompiler().Compile("HandlerModel", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join("; ", compilation.Diagnostics));
        var request = handler_model.Request(source: source) with
        {
            IdentityCatalog = compilation.Value!.Documents.IdentityCatalog,
            SemanticIdentities = compilation.Value.Documents.IdentityCatalog.Semantics
        };
        VerifyReports(new CratisArtifactRenderPlanner().Plan(request), "STAGE-ESM-011");
    }

    static void VerifyReports(ArtifactRenderPlan plan, string code)
    {
        Assert.True(plan.CanPublishArtifacts, string.Join("; ", plan.Diagnostics));
        var inventory = Assert.IsType<ArtifactRenderInventory>(plan.Inventory);
        var omitted = Assert.Single(inventory.Constructs.Where(construct => construct.Outcome == ArtifactRenderOutcome.Refused && construct.RefusalReasons.All(reason => reason.Severity == ArtifactRenderDiagnosticSeverity.Warning)));
        Assert.Equal(ArtifactRenderConstructKind.Specification, omitted.Kind);
        Assert.Equal(ArtifactRenderOutcome.Refused, omitted.Outcome);
        Assert.NotEmpty(omitted.RefusalReasons);
        Assert.Contains(omitted.RefusalReasons, reason => reason.Code == code);
        Assert.All(omitted.RefusalReasons, reason => Assert.Contains(reason, plan.Diagnostics));
        Assert.DoesNotContain(plan.Artifacts, artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.Sources.Contains(omitted.Identity));
        Assert.Equal(ArtifactRenderOutcome.Degraded, plan.Outcome);
        Assert.Equal(1, inventory.Score.Counts.Refused);
        Assert.True(inventory.Score.Counts.Rendered > 0);
        Assert.Contains(plan.Artifacts, artifact => artifact.RelativePath.EndsWith("completeness.json", StringComparison.Ordinal));
        Assert.Contains(plan.Artifacts, artifact => artifact.RelativePath == "UNSUPPORTED.md");
        Assert.Equal(plan.Outcome, plan.WithAcceptance(plan.Degradations, strict: true).Outcome);
    }
}
