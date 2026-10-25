// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_reducer_migration
{
    [Fact]
    public async Task should_refuse_evidence_for_a_model_without_exactly_one_reducer()
    {
        var loaded = await when_rendering_a_pure_reducer.Load(when_migrating_a_classic_reducer.Source);
        var evidence = when_migrating_a_classic_reducer.Evidence(loaded) with { ReadModel = loaded.Model.Application.Id };
        AssertRefused(Plan(loaded, Profile([evidence], canRemoveFiles: true)), "STAGE-REDUCER-001");
    }

    [Theory]
    [InlineData("path")]
    [InlineData("type")]
    public async Task should_refuse_evidence_that_does_not_match_the_classic_reducer(string mismatch)
    {
        var loaded = await when_rendering_a_pure_reducer.Load(when_migrating_a_classic_reducer.Source);
        var evidence = when_migrating_a_classic_reducer.Evidence(loaded);
        evidence = mismatch == "path"
            ? evidence with { RelativePath = "Orders/Ordering/Fold.cs" }
            : evidence with { ReadModelType = "Projects.Other.Total" };
        AssertRefused(Plan(loaded, Profile([evidence], canRemoveFiles: true)), "STAGE-REDUCER-002");
    }

    [Fact]
    public async Task should_refuse_a_classic_reducer_that_observes_another_event_sequence()
    {
        var loaded = await when_rendering_a_pure_reducer.Load(when_migrating_a_classic_reducer.Source);
        var classic = when_migrating_a_classic_reducer.Text(when_rendering_a_pure_reducer.Plan(loaded).Artifacts.Single(artifact => artifact.RelativePath == when_migrating_a_classic_reducer.FoldPath));
        var customized = classic.Replace("public class Fold :", "[global::Cratis.Chronicle.EventSequences.EventSequence(\"outbox\")]\npublic class Fold :", StringComparison.Ordinal);
        Assert.NotEqual(classic, customized);
        var evidence = ReducerMigrationEvidence.FromExistingFile(when_migrating_a_classic_reducer.ReadModel(loaded, "Total"), when_migrating_a_classic_reducer.FoldPath, customized);
        AssertRefused(Plan(loaded, Profile([evidence], canRemoveFiles: true)), "STAGE-REDUCER-002");
    }

    [Fact]
    public async Task should_refuse_a_host_that_cannot_remove_files()
    {
        var loaded = await when_rendering_a_pure_reducer.Load(when_migrating_a_classic_reducer.Source);
        AssertRefused(Plan(loaded, Profile([when_migrating_a_classic_reducer.Evidence(loaded)], canRemoveFiles: false)), "STAGE-REDUCER-003");
    }

    [Fact]
    public async Task should_refuse_a_scoped_migration()
    {
        var loaded = await when_rendering_a_pure_reducer.Load(when_migrating_a_classic_reducer.Source);
        var slice = loaded.Model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "Totals");
        var plan = Plan(loaded, Profile([when_migrating_a_classic_reducer.Evidence(loaded)], canRemoveFiles: true), new(ArtifactRenderScopeKind.Slice, slice.Id));
        AssertRefused(plan, "STAGE-REDUCER-004");
    }

    [Fact]
    public async Task should_refuse_a_reserved_member_on_the_migrated_model()
    {
        var loaded = await when_rendering_a_pure_reducer.Load(when_migrating_a_classic_reducer.Source.Replace("query ById", "query Reduce", StringComparison.Ordinal));
        var plan = Plan(loaded, Profile([when_migrating_a_classic_reducer.Evidence(loaded)], canRemoveFiles: true));
        AssertRefused(plan, "STAGE-ESM-019");
        rendering_refusals.Omitted(plan, when_migrating_a_classic_reducer.ReadModel(loaded, "Total"));
        rendering_refusals.Compiles(plan);
    }

    static void AssertRefused(ArtifactRenderPlan plan, string code)
    {
        rendering_refusals.Partial(plan);
        Assert.Empty(plan.RequiredRemovals);
        var diagnostics = plan.Diagnostics.Where(diagnostic => diagnostic.Code == code &&
            !diagnostic.Message.StartsWith("Generated artifact '", StringComparison.Ordinal)).ToArray();
        Assert.NotEmpty(diagnostics);
        foreach (var diagnostic in diagnostics)
        {
            rendering_refusals.Warning(plan, code, diagnostic.Artifact, diagnostic.Message);
        }
    }

    static ArtifactRenderProfile Profile(ReducerMigrationEvidence[] evidence, bool canRemoveFiles) =>
        CratisRendering.WithReducerMigration(CratisRendering.CreateProfile("Projects", new("Projects", "Projects")), "Projects", evidence, canRemoveFiles);

    static ArtifactRenderPlan Plan(LoadedSemanticModel loaded, ArtifactRenderProfile profile, ArtifactRenderScope? scope = null) =>
        new CratisArtifactRenderPlanner().Plan(new(loaded.Model, loaded.Plan, profile, scope ?? new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id))
        {
            ImplementationRequirements = loaded.ImplementationRequirements,
            ImplementationContents = loaded.ImplementationContents,
            TypedContextDescriptors = loaded.TypedContextDescriptors
        });
}
#endif
