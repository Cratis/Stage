// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_publishing_a_reducer_migration : a_generated_application
{
    const string FoldPath = when_migrating_a_classic_reducer.FoldPath;

    protected override bool DeferPlan => true;

    [Fact]
    public async Task should_remove_the_classic_reducer_and_build_the_migrated_application()
    {
        try
        {
            var migration = await RenderClassicAndPlanMigration();
            Assert.IsType<CratisPublicationCheck.Compatible>(CratisRendering.CheckPublication(migration, ReadExisting));
            PublishWithRemovals(migration);

            Assert.False(GeneratedFileExists(FoldPath));
            Assert.Contains("\"version\":2", ReadGeneratedFile(ReducerConventionInput.RelativePath), StringComparison.Ordinal);
            var build = await Run("reducer-migration-build.log", "build", "Projects.csproj", "-c", "Debug", "-t:Rebuild", "-warnaserror", "-p:NoWarn=CS7022", "--nologo");
            BuildWarnings(build).ShouldEqual(string.Empty);
        }
        finally
        {
            Cleanup();
        }
    }

    [Fact]
    public async Task should_restore_the_classic_reducer_when_a_write_fails()
    {
        try
        {
            var migration = await RenderClassicAndPlanMigration();
            var classic = ReadGeneratedFile(FoldPath);
            var model = migration.Artifacts.Single(artifact => artifact.RelativePath == "Orders/Ordering/Totals/Totals.cs");
            var previousModel = ReadGeneratedFile(model.RelativePath);
            var failAt = migration.Artifacts[^1].RelativePath;

            Assert.Throws<IOException>(() => PublishWithRemovals(migration, failAt));

            Assert.Equal(classic, ReadGeneratedFile(FoldPath));
            Assert.Equal(previousModel, ReadGeneratedFile(model.RelativePath));
        }
        finally
        {
            Cleanup();
        }
    }

    async Task<ArtifactRenderPlan> RenderClassicAndPlanMigration()
    {
        var loaded = await when_rendering_a_pure_reducer.Load(when_migrating_a_classic_reducer.Source);
        RenderApplication(when_rendering_a_pure_reducer.Plan(loaded));
        var evidence = ReducerMigrationEvidence.FromExistingFile(when_migrating_a_classic_reducer.ReadModel(loaded, "Total"), FoldPath, ReadGeneratedFile(FoldPath));
        var profile = CratisRendering.WithReducerMigration(CratisRendering.CreateProfile("Projects", new("Projects", "Projects")), "Projects", [evidence], canRemoveFiles: true);
        var migration = when_selecting_self_reducing_rendering.Plan(loaded, profile);
        Assert.True(migration.Success, string.Join(Environment.NewLine, migration.Diagnostics));
        Assert.Equal(FoldPath, Assert.Single(migration.RequiredRemovals).RelativePath);
        return migration;
    }

    string? ReadExisting(string relativePath) => GeneratedFileExists(relativePath) ? ReadGeneratedFile(relativePath) : null;
}
#endif
