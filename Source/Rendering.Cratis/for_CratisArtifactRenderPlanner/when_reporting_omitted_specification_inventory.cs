// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
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
    public void should_report_an_omitted_body_specification_expecting_more_events_than_produced_without_completion_credit()
    {
        const string source = handler_model.Source + "\n      specification PlacingOrder\n        when PlaceOrder\n          orderId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          name = \"Alice\"\n        then OrderPlaced\n          name = \"ALICE\"\n        then OrderPlaced\n          name = \"ALICE\"\n";
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_preserve_real_seeded_specification_omission_and_render_around_metadata(bool service)
    {
        var request = SeededRequest(service);
        var plan = new CratisArtifactRenderPlanner().Plan(request);
        Assert.True(plan.Success, string.Join(';', plan.Diagnostics));
        var refused = Assert.Single(plan.Refusals);
        Assert.NotEmpty(plan.Inventory!.Constructs);
        Assert.Contains(plan.Inventory.Constructs, construct => construct.Identity == refused.Artifact && construct.Outcome == ArtifactRenderOutcome.Refused);
        Assert.DoesNotContain(plan.Artifacts, artifact => artifact.Sources.Contains(refused.Artifact));
        var profile = request.Profile;
        var rootProfile = ArtifactRenderProfile.Create(profile.Target, profile.TargetVersion, profile.Renderer, profile.RendererVersion, [.. profile.Inputs.Where(input => input.Name != ServiceLayoutInput.Name)]);
        var root = new CratisArtifactRenderPlanner().Plan(request with { Profile = rootProfile });
        Assert.True(root.Success, string.Join(';', root.Diagnostics));
        Assert.Equal(root.SemanticRevision, plan.SemanticRevision);
        Assert.Equal(root.InputVersions, plan.InputVersions);
        Assert.Equal(root.Scope, plan.Scope);
        Assert.True(root.AdditionalScopes.SequenceEqual(plan.AdditionalScopes));
        Assert.Equal(JsonSerializer.Serialize(root.Refusals), JsonSerializer.Serialize(plan.Refusals));
        Assert.True(root.RequiredRemovals.SequenceEqual(plan.RequiredRemovals));
        Assert.True(root.ImplementationGaps.SequenceEqual(plan.ImplementationGaps));
        Assert.True(root.PlanEvidence.SequenceEqual(plan.PlanEvidence));
        Assert.True(root.Degradations.SequenceEqual(plan.Degradations));
        Assert.Equal(root.Outcome, plan.Outcome);
        Assert.True(root.Diagnostics.SequenceEqual(plan.Diagnostics));
        AssertReports(root, plan, service ? "Source/Projects/" : string.Empty);
        var aroundProfile = ArtifactRenderProfile.Create(profile.Target, profile.TargetVersion, profile.Renderer, profile.RendererVersion, [.. profile.Inputs.Where(input => input.Name != RefusalModeInput.Name), RefusalModeInput.Create(ArtifactRefusalMode.RenderAround)]);
        var around = new CratisArtifactRenderPlanner().Plan(request with { Profile = aroundProfile });
        Assert.True(around.CanPublishArtifacts, string.Join(';', around.Diagnostics));
        Assert.Equal(JsonSerializer.Serialize(plan.Refusals), JsonSerializer.Serialize(around.Refusals));
        Assert.DoesNotContain(around.Artifacts, artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.Sources.Contains(refused.Artifact));
    }

    static ArtifactRenderRequest SeededRequest(bool service)
    {
        var corpus = RegisterProjectCorpus.LegacyV1;
        var form = corpus.SourceForms.Single(candidate => candidate.Name == "single");
        var document = form.Documents.Single();
        var source = document.Text.Replace("      specification RegisteringAProject\n", "      specification RegisteringAProject\n        given readmodel ProjectSummary\n          projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          name = \"Screenplay\"\n", StringComparison.Ordinal);
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var input = SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, source);
        var compilation = new SemanticModelCompiler().Compile(corpus.ApplicationName, SemanticDocumentSet.Create([input], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var compiled = compilation.Value!;
        var execution = SemanticExecutionPlan.Compile(compiled.Model);
        Assert.True(execution.Success, string.Join(';', execution.Issues));
        Assert.NotEmpty(compiled.Documents.IdentityCatalog.Semantics);
        return new(compiled.Model, execution.Plan!, CratisRendering.CreateProfile(corpus.ApplicationName, new("Projects", "Projects") { ServiceLayout = service }), new(ArtifactRenderScopeKind.Application, compiled.Model.Application.Id))
        {
            IdentityCatalog = compiled.Documents.IdentityCatalog,
            SemanticIdentities = compiled.Documents.IdentityCatalog.Semantics
        };
    }

    static void AssertReports(ArtifactRenderPlan root, ArtifactRenderPlan plan, string prefix)
    {
        foreach (var path in new[] { ".stage/completeness.json", "UNSUPPORTED.md" })
        {
            var expected = root.Artifacts.Single(artifact => artifact.RelativePath == path);
            var actual = plan.Artifacts.Single(artifact => artifact.RelativePath == prefix + path);
            Assert.NotEmpty(actual.Bytes);
            Assert.True(expected.Bytes.SequenceEqual(actual.Bytes));
            Assert.True(expected.Sources.SequenceEqual(actual.Sources));
            Assert.True(new[] { plan.Scope.Artifact }.Concat(plan.AdditionalScopes.Select(scope => scope.Artifact)).ToHashSet().SetEquals(actual.Sources));
        }
        using var json = JsonDocument.Parse(plan.Artifacts.Single(artifact => artifact.RelativePath == prefix + ".stage/completeness.json").Bytes.AsMemory());
        Assert.Equal(plan.Success, json.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(plan.Strict, json.RootElement.GetProperty("strict").GetBoolean());
        Assert.Equal(plan.CanPublishArtifacts, json.RootElement.GetProperty("canPublishArtifacts").GetBoolean());
        Assert.Equal(plan.Inventory!.Constructs.Length, json.RootElement.GetProperty("inventory").GetProperty("constructs").GetArrayLength());
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
