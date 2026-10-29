// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_strict_planning_retains_performer_metadata : given.a_source_plan
{
    CratisPlanResult _normal = null!;
    ArtifactRenderPlan _beforeBaseline = null!;
    ArtifactRenderPlan _afterBaseline = null!;
    ArtifactRenderPlan _legacy = null!;
    ArtifactRenderRequest _legacyRequest = null!;
    SemanticImplementationRequirement _performerRequirement = null!;
    SemanticIdentityCatalog _catalog = null!;
    ArtifactRenderPlan _defaultLegacy = null!;
    ArtifactRenderPlan _defaultAccepted = null!;

    void Establish() => File.WriteAllText(Path.Combine(_root, "performer.play"), """
        concept Status : Enum
          draft
          validate
          sent
        module Projects
          feature Registration
            slice StateView Listings
              readmodel PersonSummary
                description "Person summary"
                personId Uuid
                name String
              query FindPeople => PersonSummary[]
                filter name String
                performer
                  ```sql
                  select * from people
                  ```
        """);

    async Task Because()
    {
        _normal = await From("performer.play");
        _planOptions = _planOptions with { Strict = true };
        _result = await From("performer.play");
        var loaded = await SemanticModelLoader.LoadAsync(_root, [], null, _planOptions.ApplicationName);
        Assert.True(loaded.Success, string.Join("; ", loaded.Diagnostics));
        var model = loaded.Loaded!;
        Assert.NotNull(model.IdentityCatalog);
        _catalog = model.IdentityCatalog;
        var projects = model.Model.Application.Modules.Single(module => module.Name == "Projects");
        var tasks = model.Model.Application.Modules.Single(module => module.Name == "Tasks");
        _performerRequirement = model.ImplementationRequirements.Single(requirement => requirement.Role == SemanticImplementationRole.QueryPerformer);
        var request = new ArtifactRenderRequest(
            model.Model,
            model.Plan,
            CratisRendering.CreateProfile(_planOptions.ApplicationName, new(_planOptions.ProjectName, _planOptions.RootNamespace) { Strict = true }),
            new(ArtifactRenderScopeKind.Feature, projects.Features.Single(feature => feature.Name == "Registration").Id))
        {
            AdditionalScopes = [new(ArtifactRenderScopeKind.Module, tasks.Id)],
            SemanticIdentities = model.SemanticIdentities,
            IdentityCatalog = model.IdentityCatalog,
            ImplementationRequirements = model.ImplementationRequirements,
            ImplementationContents = model.ImplementationContents,
            TypedContextDescriptors = model.TypedContextDescriptors,
            AttachmentDiagnostics = model.AttachmentDiagnostics
        };
        var planner = new CratisArtifactRenderPlanner();
        var defaults = new CratisRenderingOptions(_planOptions.ProjectName, _planOptions.RootNamespace);
        var defaultRequest = request with
        {
            Profile = CratisRendering.CreateProfile(_planOptions.ApplicationName, defaults),
            SemanticIdentities = [],
            IdentityCatalog = null
        };
        _defaultLegacy = planner.Plan(defaultRequest);
        _defaultAccepted = CratisPlanAcceptance.Apply(defaultRequest, _defaultLegacy, defaults);
        _legacyRequest = request with { SemanticIdentities = [], IdentityCatalog = null };
        _legacy = planner.Plan(_legacyRequest);
        _beforeBaseline = planner.Plan(request);
        _afterBaseline = planner.Plan(request with { ReleaseBaseline = new(ReleaseBaselineAssessmentStatus.Unreadable, model.Model.Revision, [], "fixture unreadable baseline") });
    }

    [Fact]
    void should_leave_default_acceptance_and_legacy_performer_diagnostics_and_digest_unchanged()
    {
        Assert.NotEmpty(_defaultLegacy.ImplementationGaps);
        Assert.Contains(_defaultLegacy.Diagnostics, diagnostic => diagnostic.Code == "STAGE-IMPL-006");
        Assert.DoesNotContain(_defaultLegacy.Diagnostics, diagnostic => diagnostic.Code.StartsWith("STAGE-DEGRADE-", StringComparison.Ordinal));
        Assert.Empty(_defaultLegacy.Degradations);
        Assert.False(_defaultLegacy.Strict);
        Assert.Same(_defaultLegacy, _defaultAccepted);
        Assert.Equal("ae80e507058f7bdd29d97182d1b8b411a05d01e2eb0def81fe5dab967d304830", _defaultLegacy.Digest);
        Assert.DoesNotContain(_normal.Plan!.Diagnostics, diagnostic => diagnostic.Code.StartsWith("STAGE-DEGRADE-", StringComparison.Ordinal));
        Assert.Equal(ArtifactRenderOutcome.Partial, _normal.Plan.Outcome);
        Assert.Null(_normal.Plan.Inventory);
        Assert.DoesNotContain(_normal.Plan.Artifacts, IsReport);
    }

    [Fact]
    void should_keep_safe_generated_files_while_rejecting_strict_acceptance()
    {
        Assert.NotNull(_normal.Plan);
        Assert.NotEmpty(_normal.Artifacts);
        Assert.True(_normal.Plan.Success);
        Assert.True(_result.Plan!.Strict);
        Assert.True(_result.Plan.CanPublishArtifacts);
        Assert.False(_result.Plan.Success);
        Assert.Equal(ArtifactRenderOutcome.Degraded, _result.Plan.Outcome);
        Assert.NotEmpty(_result.Plan.ImplementationGaps);
        Assert.NotEmpty(_result.Plan.Degradations);
        AssertPayload(_normal.Plan, _result.Plan);
    }

    [Fact]
    void should_preserve_nonempty_metadata_through_the_real_source_mapping_route()
    {
        var source = _result.Diagnostics.Where(diagnostic => diagnostic.Code == "PLAY0270").ToArray();
        Assert.NotEmpty(source);
        foreach (var diagnostic in source)
        {
            Assert.NotNull(diagnostic.Source);
            Assert.Contains(_result.Plan!.Diagnostics, actual => actual.Code == diagnostic.Code && actual.Message == $"{diagnostic.Source}: {diagnostic.Message}");
        }
        Assert.Contains(_result.Plan!.Diagnostics, diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning);
        var original = _result.Plan.WithAcceptance(_result.Plan.Degradations);
        Assert.All(original.Degradations, evidence => Assert.Contains(evidence.Diagnostic, original.Diagnostics));
        Assert.Equal(_normal.Plan!.InputVersions, original.InputVersions);
        Assert.Equal(_normal.Plan.SemanticRevision, original.SemanticRevision);
        Assert.NotEqual(_normal.Plan.Digest, _result.Plan.Digest);
    }

    [Fact]
    void should_preserve_nonempty_gaps_scopes_and_exact_payload_through_the_real_baseline_route()
    {
        Assert.NotEmpty(_beforeBaseline.Artifacts);
        Assert.NotEmpty(_beforeBaseline.ImplementationGaps);
        Assert.NotEmpty(_beforeBaseline.AdditionalScopes);
        Assert.Contains(_afterBaseline.Diagnostics, diagnostic => diagnostic.Code == "STAGE-BASELINE-003" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning);
        Assert.NotEqual(_beforeBaseline.Digest, _afterBaseline.Digest);
        Assert.True(_afterBaseline.CanPublishArtifacts);
        Assert.False(_afterBaseline.Success);
        AssertPayload(_beforeBaseline, _afterBaseline);
        Assert.Equal(JsonSerializer.Serialize(_beforeBaseline.ImplementationGaps), JsonSerializer.Serialize(_afterBaseline.ImplementationGaps));
        Assert.True(_beforeBaseline.Degradations.SequenceEqual(_afterBaseline.Degradations));
        Assert.Equal(_beforeBaseline.Scope, _afterBaseline.Scope);
        Assert.True(_beforeBaseline.AdditionalScopes.SequenceEqual(_afterBaseline.AdditionalScopes));
        Assert.Equal(_beforeBaseline.InputVersions, _afterBaseline.InputVersions);
        Assert.Equal(_beforeBaseline.SemanticRevision, _afterBaseline.SemanticRevision);
        Assert.Equal(_beforeBaseline.Target, _afterBaseline.Target);
        Assert.Equal(_beforeBaseline.TargetVersion, _afterBaseline.TargetVersion);
        Assert.Equal(_beforeBaseline.Renderer, _afterBaseline.Renderer);
        Assert.Equal(_beforeBaseline.RendererVersion, _afterBaseline.RendererVersion);
    }

    [Fact]
    void should_transport_exact_catalog_locations_into_scoped_managed_reports()
    {
        var plan = _result.Plan!;
        Assert.NotNull(plan.Inventory);
        Assert.NotEmpty(plan.Inventory.Constructs);
        var query = plan.Inventory.Constructs.Single(construct => construct.Identity == _performerRequirement.Source.SemanticId);
        Assert.Equal(_performerRequirement.Owner, query.SemanticLocator);
        Assert.Equal(query.Identity, _catalog.ResolveSemanticAssignment(query.SemanticLocator).Id);
        Assert.Null(query.ModelPath);
        Assert.All(query.Degradations, gap => Assert.Equal(query.SemanticLocator, gap.SemanticLocator));
        Assert.All(plan.Inventory.Constructs, construct => Assert.Null(construct.ModelPath));
        Assert.DoesNotContain(plan.Inventory.Constructs, construct => construct.SemanticLocator.Kind is SemanticKind.Application or SemanticKind.Module or SemanticKind.Feature or SemanticKind.Slice);
        var report = plan.Artifacts.Single(artifact => artifact.RelativePath == ".stage/completeness.json");
        Assert.NotEmpty(report.Bytes);
        Assert.NotEmpty(plan.Artifacts.Single(artifact => artifact.RelativePath == "UNSUPPORTED.md").Bytes);
        using var json = JsonDocument.Parse(report.Bytes.AsMemory());
        Assert.True(json.RootElement.GetProperty("strict").GetBoolean());
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.True(json.RootElement.GetProperty("canPublishArtifacts").GetBoolean());
        Assert.Equal(ArtifactRenderReports.Create(plan, new(plan.InputVersions!, new([plan.InputVersions!]), [new(plan.Renderer, plan.RendererVersion)])).Completeness.Sha256, report.Sha256);
        Assert.All(report.Sources, source => Assert.Contains(new[] { plan.Scope }.Concat(plan.AdditionalScopes), scope => scope.Artifact == source));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_preserve_the_real_strict_sql_failure_in_each_layout(bool service)
    {
        _planOptions = _planOptions with { Strict = true, ServiceLayout = false };
        var root = await From("performer.play");
        _planOptions = _planOptions with { ServiceLayout = service };
        var result = await From("performer.play");
        var expected = Assert.IsType<ArtifactRenderPlan>(root.Plan);
        var plan = Assert.IsType<ArtifactRenderPlan>(result.Plan);
        Assert.False(root.Success);
        Assert.False(result.Success);
        Assert.True(plan.CanPublishArtifacts);
        Assert.True(plan.Strict);
        Assert.Equal(ArtifactRenderOutcome.Degraded, plan.Outcome);
        Assert.NotEmpty(plan.Degradations);
        Assert.NotEmpty(plan.ImplementationGaps);
        Assert.NotEmpty(result.Artifacts);
        Assert.Equal(expected.SemanticRevision, plan.SemanticRevision);
        Assert.Equal(expected.InputVersions, plan.InputVersions);
        Assert.Equal(expected.Scope, plan.Scope);
        Assert.True(expected.AdditionalScopes.SequenceEqual(plan.AdditionalScopes));
        Assert.True(expected.Degradations.SequenceEqual(plan.Degradations));
        Assert.True(expected.PlanEvidence.SequenceEqual(plan.PlanEvidence));
        Assert.Equal(JsonSerializer.Serialize(expected.Refusals), JsonSerializer.Serialize(plan.Refusals));
        Assert.True(expected.RequiredRemovals.SequenceEqual(plan.RequiredRemovals));
        Assert.True(expected.Diagnostics.SequenceEqual(plan.Diagnostics));
        Assert.Equal(plan.Digest, result.Digest);
        Assert.True(result.Artifacts.SequenceEqual(plan.Artifacts));
        Assert.True(result.ImplementationGaps.SequenceEqual(plan.ImplementationGaps));
        Assert.True(result.RequiredRemovals.SequenceEqual(plan.RequiredRemovals));
        Assert.True(result.Refusals.SequenceEqual(plan.Refusals));
        AssertStrictReports(expected, plan, service ? $"Source/{_planOptions.ProjectName}/" : string.Empty);
    }

    static void AssertStrictReports(ArtifactRenderPlan root, ArtifactRenderPlan plan, string prefix)
    {
        foreach (var path in new[] { ".stage/completeness.json", "UNSUPPORTED.md" })
        {
            var expected = root.Artifacts.Single(artifact => artifact.RelativePath == path);
            var report = plan.Artifacts.Single(artifact => artifact.RelativePath == prefix + path);
            Assert.NotEmpty(report.Bytes);
            Assert.True(expected.Bytes.SequenceEqual(report.Bytes));
            Assert.True(expected.Sources.SequenceEqual(report.Sources));
            Assert.True(new[] { plan.Scope.Artifact }.Concat(plan.AdditionalScopes.Select(scope => scope.Artifact)).ToHashSet().SetEquals(report.Sources));
        }
        Assert.NotEmpty(plan.Inventory!.Constructs.SelectMany(construct => construct.Degradations));
        using var json = JsonDocument.Parse(plan.Artifacts.Single(artifact => artifact.RelativePath == prefix + ".stage/completeness.json").Bytes.AsMemory());
        Assert.True(json.RootElement.GetProperty("strict").GetBoolean());
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.True(json.RootElement.GetProperty("canPublishArtifacts").GetBoolean());
        Assert.Equal("Degraded", json.RootElement.GetProperty("outcome").GetString());
        Assert.Equal(root.Inventory!.Constructs.Select(construct => (construct.Identity, construct.Outcome)), plan.Inventory.Constructs.Select(construct => (construct.Identity, construct.Outcome)));
        Assert.Equal(plan.Inventory.Constructs.Length, json.RootElement.GetProperty("inventory").GetProperty("constructs").GetArrayLength());
        foreach (var artifact in plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal)))
        {
            Assert.StartsWith(prefix, artifact.RelativePath, StringComparison.Ordinal);
            var original = root.Artifacts.Single(candidate => prefix + candidate.RelativePath == artifact.RelativePath);
            Assert.True(original.Bytes.SequenceEqual(artifact.Bytes));
            Assert.True(original.Sources.SequenceEqual(artifact.Sources));
        }
    }

    [Fact]
    void should_leave_direct_models_without_catalogs_unreported_without_fabrication()
    {
        Assert.Null(_legacy.Inventory);
        Assert.DoesNotContain(_legacy.Artifacts, IsReport);
        Assert.Same(_legacy, CratisPlanReports.Finalize(_legacyRequest, _legacy, new("unused", "unused")));
    }

    static bool IsReport(PlannedArtifact artifact) => string.Equals(artifact.RelativePath, ".stage/completeness.json", StringComparison.Ordinal) || string.Equals(artifact.RelativePath, "UNSUPPORTED.md", StringComparison.Ordinal);

    static void AssertPayload(ArtifactRenderPlan expected, ArtifactRenderPlan actual)
    {
        var leftArtifacts = expected.Artifacts.Where(artifact => !IsReport(artifact)).ToArray();
        var rightArtifacts = actual.Artifacts.Where(artifact => !IsReport(artifact)).ToArray();
        Assert.Equal(leftArtifacts.Length, rightArtifacts.Length);
        foreach (var (left, right) in leftArtifacts.Zip(rightArtifacts))
        {
            Assert.Equal(left.Kind, right.Kind);
            Assert.Equal(left.RelativePath, right.RelativePath);
            Assert.Equal(left.Sha256, right.Sha256);
            Assert.True(left.Bytes.SequenceEqual(right.Bytes));
            Assert.True(left.Sources.SequenceEqual(right.Sources));
        }
    }
}
