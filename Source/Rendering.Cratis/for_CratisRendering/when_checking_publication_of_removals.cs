// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_checking_publication_of_removals
{
    const string FoldPath = when_migrating_a_classic_reducer.FoldPath;

    [Fact]
    public async Task should_be_compatible_when_the_removed_file_matches_or_is_absent()
    {
        var (plan, evidence) = await Migrated();
        var classic = await ClassicFold();
        Assert.Equal(evidence.Sha256, PlannedArtifact.CreateText(FoldPath, classic).Sha256);
        Assert.IsType<CratisPublicationCheck.Compatible>(CratisRendering.CheckPublication(plan, path => path == FoldPath ? classic : null));
        Assert.IsType<CratisPublicationCheck.Compatible>(CratisRendering.CheckPublication(plan, _ => null));
    }

    [Fact]
    public async Task should_refuse_a_removal_whose_file_changed()
    {
        var (plan, _) = await Migrated();
        var changed = await ClassicFold() + "// edited\n";
        var check = Assert.IsType<CratisPublicationCheck.UnsafeRemovals>(CratisRendering.CheckPublication(plan, path => path == FoldPath ? changed : null));
        var removal = Assert.Single(check.Removals);
        Assert.Equal((FoldPath, PlannedArtifact.CreateText(FoldPath, changed).Sha256), (removal.RelativePath, removal.ExpectedSha256));
        Assert.All(check.Diagnostics, diagnostic => Assert.Equal("STAGE-REDUCER-005", diagnostic.Code));
    }

    [Fact]
    public async Task should_refuse_an_application_plan_that_leaves_the_classic_reducer()
    {
        var loaded = await when_rendering_a_pure_reducer.Load(when_migrating_a_classic_reducer.Source);
        var replanned = when_selecting_self_reducing_rendering.Plan(loaded, PersistedProfile(loaded));
        var classic = await ClassicFold();
        var check = Assert.IsType<CratisPublicationCheck.UnsafeRemovals>(CratisRendering.CheckPublication(replanned, path => path == FoldPath ? classic : null));
        Assert.Equal(FoldPath, Assert.Single(check.Removals).RelativePath);
        Assert.Equal("STAGE-REDUCER-005", Assert.Single(check.Diagnostics).Code);
        Assert.IsType<CratisPublicationCheck.Compatible>(CratisRendering.CheckPublication(replanned, _ => null));
    }

    [Fact]
    public async Task should_refuse_a_scoped_plan_beside_a_destination_that_keeps_the_classic_reducer()
    {
        var loaded = await when_rendering_a_pure_reducer.Load(when_migrating_a_classic_reducer.Source);
        var (migrated, _) = when_migrating_a_classic_reducer.Migrate(loaded);
        var config = when_migrating_a_classic_reducer.Text(migrated.Artifacts.Single(artifact => artifact.RelativePath == ReducerConventionInput.RelativePath));
        var slice = loaded.Model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "Totals");
        var scoped = new CratisArtifactRenderPlanner().Plan(new(loaded.Model, loaded.Plan, when_migrating_a_classic_reducer.Persisted(config), new(ArtifactRenderScopeKind.Slice, slice.Id))
        {
            ImplementationRequirements = loaded.ImplementationRequirements,
            ImplementationContents = loaded.ImplementationContents,
            TypedContextDescriptors = loaded.TypedContextDescriptors
        });
        Assert.True(scoped.Success, string.Join(Environment.NewLine, scoped.Diagnostics));
        var classic = await ClassicFold();
        var existing = new Dictionary<string, string>(StringComparer.Ordinal) { [FoldPath] = classic, [ReducerConventionInput.RelativePath] = config };
        string? Read(string path) => existing.GetValueOrDefault(path);
        Assert.IsType<CratisPublicationCheck.UnsafeRemovals>(CratisRendering.CheckPublication(scoped, Read));
    }

    static async Task<(ArtifactRenderPlan Plan, ReducerMigrationEvidence Evidence)> Migrated()
    {
        var (plan, evidence) = when_migrating_a_classic_reducer.Migrate(await when_rendering_a_pure_reducer.Load(when_migrating_a_classic_reducer.Source));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        return (plan, evidence);
    }

    static async Task<string> ClassicFold()
    {
        var classic = when_rendering_a_pure_reducer.Plan(await when_rendering_a_pure_reducer.Load(when_migrating_a_classic_reducer.Source));
        return when_migrating_a_classic_reducer.Text(classic.Artifacts.Single(artifact => artifact.RelativePath == FoldPath));
    }

    static ArtifactRenderProfile PersistedProfile(LoadedSemanticModel loaded)
    {
        var (migrated, _) = when_migrating_a_classic_reducer.Migrate(loaded);
        return when_migrating_a_classic_reducer.Persisted(when_migrating_a_classic_reducer.Text(migrated.Artifacts.Single(artifact => artifact.RelativePath == ReducerConventionInput.RelativePath)));
    }
}
#endif
