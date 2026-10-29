// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_migrating_a_classic_reducer
{
    internal const string ObserverId = "Projects.Orders.Ordering.Totals.Fold";
    internal const string FoldPath = "Orders/Ordering/Totals/Fold.cs";
    const string OriginalBody = "return new Total(Guid.Parse(\"00000000-0000-0000-0000-000000000001\"), context.Event.Amount);";
    const string Body = "return new Total(context.Event.Id, context.Event.Amount);";

    internal static string Source => when_rendering_a_pure_reducer.Source.Replace(OriginalBody, Body, StringComparison.Ordinal);

    // A second read model in the same slice that stays classic beside the migrated one.
    internal static string MixedSource => Source.Replace(
        "      reducer Fold => Total",
        "      readmodel Count\n        id Uuid\n        orders Decimal\n      query CountById => Count?\n        by id Uuid\n      reducer Tally => Count\n        on OrderPlaced\n          ```csharp\n          return new Count(context.Event.Id, 1);\n          ```\n      reducer Fold => Total",
        StringComparison.Ordinal);

    [Theory]
    [InlineData("StateView")]
    [InlineData("Automation")]
    public async Task should_render_a_self_reducing_model_that_keeps_the_observer_identity(string kind)
    {
        var loaded = await when_rendering_a_pure_reducer.Load(Source.Replace("slice StateView Totals", $"slice {kind} Totals", StringComparison.Ordinal));
        var (plan, evidence) = Migrate(loaded);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        Assert.Equal(ObserverId, evidence.ObserverId);
        Assert.DoesNotContain(plan.Artifacts, artifact => artifact.RelativePath == FoldPath);
        var model = Text(plan.Artifacts.Single(artifact => artifact.RelativePath == "Orders/Ordering/Totals/Totals.cs"));
        Assert.Contains($"[Reducer(\"{ObserverId}\")]", model, StringComparison.Ordinal);
        Assert.Contains("public static Total? Reduce(", model, StringComparison.Ordinal);
        Assert.DoesNotContain("IReducerFor", model, StringComparison.Ordinal);
        var removal = Assert.Single(plan.RequiredRemovals);
        Assert.Equal((FoldPath, evidence.Sha256, ReadModel(loaded, "Total")), (removal.RelativePath, removal.ExpectedSha256, removal.Model));
        Assert.Contains("\"version\":2", Text(plan.Artifacts.Single(artifact => artifact.RelativePath == ReducerConventionInput.RelativePath)), StringComparison.Ordinal);
        Assert.Empty(RenderedOutput.Errors(when_selecting_self_reducing_rendering.Files(plan)));
    }

    [Fact]
    public async Task should_replan_identical_bytes_without_removals_from_the_persisted_override()
    {
        var loaded = await when_rendering_a_pure_reducer.Load(Source);
        var (migrated, _) = Migrate(loaded);
        var persisted = Text(migrated.Artifacts.Single(artifact => artifact.RelativePath == ReducerConventionInput.RelativePath));
        var replanned = when_selecting_self_reducing_rendering.Plan(loaded, Persisted(persisted));
        Assert.True(replanned.Success, string.Join(Environment.NewLine, replanned.Diagnostics));
        Assert.Empty(replanned.RequiredRemovals);
        Assert.Equal(migrated.Artifacts.Select(artifact => (artifact.RelativePath, artifact.Sha256)), replanned.Artifacts.Select(artifact => (artifact.RelativePath, artifact.Sha256)));
    }

    [Fact]
    public async Task should_keep_other_read_models_in_the_slice_classic()
    {
        var loaded = await when_rendering_a_pure_reducer.Load(MixedSource);
        var (plan, _) = Migrate(loaded);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        Assert.DoesNotContain(plan.Artifacts, artifact => artifact.RelativePath == FoldPath);
        var tally = Text(plan.Artifacts.Single(artifact => artifact.RelativePath == "Orders/Ordering/Totals/Tally.cs"));
        Assert.Contains("IReducerFor<Count>", tally, StringComparison.Ordinal);
        Assert.Single(plan.RequiredRemovals);
        Assert.Empty(RenderedOutput.Errors(when_selecting_self_reducing_rendering.Files(plan)));
    }

    internal static (ArtifactRenderPlan Plan, ReducerMigrationEvidence Evidence) Migrate(LoadedSemanticModel loaded, bool canRemoveFiles = true)
    {
        var evidence = Evidence(loaded);
        var profile = CratisRendering.WithReducerMigration(CratisRendering.CreateProfile("Projects", new("Projects", "Projects")), "Projects", [evidence], canRemoveFiles);
        return (when_selecting_self_reducing_rendering.Plan(loaded, profile), evidence);
    }

    internal static ReducerMigrationEvidence Evidence(LoadedSemanticModel loaded)
    {
        var classic = when_rendering_a_pure_reducer.Plan(loaded);
        Assert.True(classic.Success, string.Join(Environment.NewLine, classic.Diagnostics));
        return ReducerMigrationEvidence.FromExistingFile(ReadModel(loaded, "Total"), FoldPath, Text(classic.Artifacts.Single(artifact => artifact.RelativePath == FoldPath)));
    }

    internal static ArtifactRenderProfile Persisted(string json) =>
        CratisRendering.WithReducerConvention(CratisRendering.CreateProfile("Projects", new("Projects", "Projects")), ReducerConventionInput.Read("Projects", json));

    internal static SemanticId ReadModel(LoadedSemanticModel loaded, string name) =>
        loaded.Model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices)
            .SelectMany(slice => slice.ReadModels).Single(readModel => readModel.Name == name).Id;

    internal static string Text(PlannedArtifact artifact) => Encoding.UTF8.GetString(artifact.Bytes.AsSpan());
}
#endif
