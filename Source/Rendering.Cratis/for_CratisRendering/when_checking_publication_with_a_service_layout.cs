// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_checking_publication_with_a_service_layout
{
    const string Folder = "Source/Projects/";
    const string FoldPath = when_migrating_a_classic_reducer.FoldPath;

    [Fact]
    public async Task should_place_the_required_removal_in_the_service_folder()
    {
        var (plan, evidence, _) = await Migrated();
        var removal = Assert.Single(plan.RequiredRemovals);
        Assert.Equal(Folder + FoldPath, removal.RelativePath);
        Assert.Equal(evidence.Sha256, removal.ExpectedSha256);
    }

    [Fact]
    public async Task should_check_the_removal_where_the_service_keeps_the_file()
    {
        var (plan, _, classic) = await Migrated();
        Assert.IsType<CratisPublicationCheck.Compatible>(CratisRendering.CheckPublication(plan, path => path == Folder + FoldPath ? classic : null));
        Assert.IsType<CratisPublicationCheck.Compatible>(CratisRendering.CheckPublication(plan, _ => null));
        var changed = classic + "// edited\n";
        var check = Assert.IsType<CratisPublicationCheck.UnsafeRemovals>(CratisRendering.CheckPublication(plan, path => path == Folder + FoldPath ? changed : null));
        Assert.Equal(Folder + FoldPath, Assert.Single(check.Removals).RelativePath);
    }

    [Fact]
    public async Task should_find_a_classic_reducer_left_in_the_service_folder()
    {
        var loaded = await when_rendering_a_pure_reducer.Load(when_migrating_a_classic_reducer.Source);
        var (migrated, _, classic) = await Migrated();
        var config = when_migrating_a_classic_reducer.Text(migrated.Artifacts.Single(artifact => artifact.RelativePath == Folder + ReducerConventionInput.RelativePath));
        var persisted = CratisRendering.WithReducerConvention(CratisRendering.CreateProfile("Projects", new("Projects", "Projects") { ServiceLayout = true }), ReducerConventionInput.Read("Projects", config));
        var replanned = when_selecting_self_reducing_rendering.Plan(loaded, persisted);
        Assert.True(replanned.Success, string.Join(Environment.NewLine, replanned.Diagnostics));
        var check = Assert.IsType<CratisPublicationCheck.UnsafeRemovals>(CratisRendering.CheckPublication(replanned, path => path == Folder + FoldPath ? classic : null));
        Assert.Equal(Folder + FoldPath, Assert.Single(check.Removals).RelativePath);
        Assert.IsType<CratisPublicationCheck.Compatible>(CratisRendering.CheckPublication(replanned, _ => null));
    }

    [Fact]
    public async Task should_never_read_a_classic_reducer_left_at_the_repository_root()
    {
        var loaded = await when_rendering_a_pure_reducer.Load(when_migrating_a_classic_reducer.Source);
        var (migrated, _, classic) = await Migrated();
        var config = when_migrating_a_classic_reducer.Text(migrated.Artifacts.Single(artifact => artifact.RelativePath == Folder + ReducerConventionInput.RelativePath));
        var persisted = CratisRendering.WithReducerConvention(CratisRendering.CreateProfile("Projects", new("Projects", "Projects") { ServiceLayout = true }), ReducerConventionInput.Read("Projects", config));
        var replanned = when_selecting_self_reducing_rendering.Plan(loaded, persisted);
        var read = new List<string>();
        var check = CratisRendering.CheckPublication(replanned, path =>
        {
            read.Add(path);
            return path == FoldPath ? classic : null;
        });
        Assert.IsType<CratisPublicationCheck.Compatible>(check);
        Assert.DoesNotContain(FoldPath, read);
    }

    static async Task<(ArtifactRenderPlan Plan, ReducerMigrationEvidence Evidence, string Classic)> Migrated()
    {
        var loaded = await when_rendering_a_pure_reducer.Load(when_migrating_a_classic_reducer.Source);
        var evidence = when_migrating_a_classic_reducer.Evidence(loaded);
        var profile = CratisRendering.WithReducerMigration(CratisRendering.CreateProfile("Projects", new("Projects", "Projects") { ServiceLayout = true }), "Projects", [evidence], true);
        var plan = when_selecting_self_reducing_rendering.Plan(loaded, profile);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var classicPlan = when_rendering_a_pure_reducer.Plan(loaded);
        var classic = when_migrating_a_classic_reducer.Text(classicPlan.Artifacts.Single(artifact => artifact.RelativePath == FoldPath));
        return (plan, evidence, classic);
    }
}
#endif
