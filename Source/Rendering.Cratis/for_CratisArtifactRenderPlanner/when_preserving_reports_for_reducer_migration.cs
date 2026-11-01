// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_preserving_reports_for_reducer_migration
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_preserve_real_migration_removals_and_suppress_the_classic_reducer(bool service)
    {
        var loaded = await when_rendering_a_pure_reducer.Load(when_migrating_a_classic_reducer.Source);
        Assert.NotNull(loaded.IdentityCatalog);
        var evidence = when_migrating_a_classic_reducer.Evidence(loaded);
        var original = new CratisArtifactRenderPlanner().Plan(Request(loaded, Profile(evidence, service: false, canRemoveFiles: true)));
        var plan = new CratisArtifactRenderPlanner().Plan(Request(loaded, Profile(evidence, service, canRemoveFiles: true)));
        Assert.True(original.Success, string.Join(';', original.Diagnostics));
        Assert.True(plan.Success, string.Join(';', plan.Diagnostics));
        var prefix = service ? "Source/Projects/" : string.Empty;
        var removal = Assert.Single(plan.RequiredRemovals);
        Assert.Equal(prefix + when_migrating_a_classic_reducer.FoldPath, removal.RelativePath);
        Assert.Equal(evidence.Sha256, removal.ExpectedSha256);
        Assert.Equal(evidence.ReadModel, removal.Model);
        var classic = new CratisArtifactRenderPlanner().Plan(Request(loaded, CratisRendering.CreateProfile("Projects", new("Projects", "Projects") { ServiceLayout = service })));
        Assert.True(classic.Success, string.Join(';', classic.Diagnostics));
        Assert.Equal(removal.ExpectedSha256, classic.Artifacts.Single(artifact => artifact.RelativePath == removal.RelativePath).Sha256);
        Assert.DoesNotContain(plan.Artifacts, artifact => artifact.RelativePath == removal.RelativePath);
        Assert.Contains(plan.Artifacts, artifact => artifact.RelativePath == prefix + "Orders/Ordering/Totals/Totals.cs");
        Assert.NotEmpty(plan.Inventory!.Constructs);
        Assert.True(plan.CanPublishArtifacts);
        Assert.Equal(original.Outcome, plan.Outcome);
        Assert.Equal(original.SemanticRevision, plan.SemanticRevision);
        Assert.Equal(original.InputVersions, plan.InputVersions);
        Assert.Equal(original.Scope, plan.Scope);
        Assert.True(original.AdditionalScopes.SequenceEqual(plan.AdditionalScopes));
        Assert.True(original.PlanEvidence.SequenceEqual(plan.PlanEvidence));
        Assert.True(original.Degradations.SequenceEqual(plan.Degradations));
        Assert.Equal(JsonSerializer.Serialize(original.Refusals), JsonSerializer.Serialize(plan.Refusals));
        AssertReports(original, plan, prefix);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_preserve_a_real_cannot_remove_refusal_without_publishing_reports_or_artifacts(bool service)
    {
        var loaded = await when_rendering_a_pure_reducer.Load(when_migrating_a_classic_reducer.Source);
        Assert.NotNull(loaded.IdentityCatalog);
        var evidence = when_migrating_a_classic_reducer.Evidence(loaded);
        var plan = new CratisArtifactRenderPlanner().Plan(Request(loaded, Profile(evidence, service, canRemoveFiles: false)));
        Assert.False(plan.Success);
        Assert.False(plan.CanPublishArtifacts);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-REDUCER-003");
        Assert.Empty(plan.Artifacts);
        Assert.Empty(plan.RequiredRemovals);
        Assert.Null(plan.Inventory);
        Assert.Empty(plan.Refusals);
        Assert.Empty(plan.ImplementationGaps);
    }

    static ArtifactRenderProfile Profile(ReducerMigrationEvidence evidence, bool service, bool canRemoveFiles) =>
        CratisRendering.WithReducerMigration(CratisRendering.CreateProfile("Projects", new("Projects", "Projects") { ServiceLayout = service }), "Projects", [evidence], canRemoveFiles);

    static ArtifactRenderRequest Request(LoadedSemanticModel loaded, ArtifactRenderProfile profile) =>
        new(loaded.Model, loaded.Plan, profile, new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id))
        {
            IdentityCatalog = loaded.IdentityCatalog,
            SemanticIdentities = loaded.SemanticIdentities,
            ImplementationRequirements = loaded.ImplementationRequirements,
            ImplementationContents = loaded.ImplementationContents,
            TypedContextDescriptors = loaded.TypedContextDescriptors,
            AttachmentDiagnostics = loaded.AttachmentDiagnostics
        };

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
        Assert.Equal(plan.Inventory!.Constructs.Select(construct => construct.Identity.ToString()), json.RootElement.GetProperty("inventory").GetProperty("constructs").EnumerateArray().Select(construct => construct.GetProperty("identity").GetString()));
    }
}
#endif
