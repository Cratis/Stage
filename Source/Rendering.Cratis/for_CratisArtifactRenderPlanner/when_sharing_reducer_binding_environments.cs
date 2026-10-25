// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_sharing_reducer_binding_environments
{
    const string OriginalBody = "return new Total(Guid.Parse(\"00000000-0000-0000-0000-000000000001\"), context.Event.Amount);";
    const string Body = "return new Total(Id: context.Event.Id, Amount: context.Event.Amount);";
    const string Adjustment = """
            slice StateChange RecordAdjustment
              command RegisterAdjustment
                id Uuid identifier
                amount Decimal
                produces AdjustmentRecorded
                  for id
                  id = id
                  amount = amount
              event AdjustmentRecorded
                id Uuid
                amount Decimal
        """;
    const string SiblingReducer = """
              readmodel Balance
                id Uuid
                amount Decimal
              query BalanceById => Balance?
                by id Uuid
              reducer FoldBalance => Balance
                on AdjustmentRecorded
                  ```csharp
                  return new Balance(Id: context.Event.Id, Amount: context.Event.Amount);
                  ```
        """;
    const string SiblingProjection = """
              readmodel Snapshot
                id Uuid
                amount Decimal
              query SnapshotById => Snapshot?
                by id Uuid
              projection SnapshotProjection => Snapshot
                from AdjustmentRecorded key id
                  id = id
                  amount = amount
        """;
    const string AutomationCommand = """
              command RecordAgain
                id Uuid identifier
                amount Decimal
                produces AdjustmentRecorded
                  for id
                  id = id
                  amount = amount
        """;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_preserve_purity_parity_with_snapshot_and_observable_queries(bool self)
    {
        var source = Source().Replace("      query ById", "      query All => observable Total[]\n      query ById", StringComparison.Ordinal);
        await AssertParity(source, self);
        var impure = source.Replace(Body, "_ = Total.All(null!); return context.State;", StringComparison.Ordinal);
        var refused = when_rendering_a_pure_reducer.Plan(await when_rendering_a_pure_reducer.Load(impure), self: self);
        Assert.False(refused.Success);
        Assert.Empty(refused.Artifacts);
        Assert.DoesNotContain(refused.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-021");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_preserve_purity_parity_with_a_sibling_projection(bool self) =>
        await AssertParity(when_rendering_a_pure_reducer.WithIdentifierConcept(Source(SiblingProjection)), self);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_preserve_purity_parity_for_every_reducer_in_the_combined_file(bool self) =>
        await AssertParity(Source(SiblingReducer), self);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_preserve_purity_parity_for_automation_command_imports(bool self) =>
        await AssertParity(Source(AutomationCommand).Replace("slice StateView Totals", "slice Automation Totals", StringComparison.Ordinal), self);

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task should_refuse_imported_short_name_ambiguities_exactly_where_the_real_file_imports_them(bool self, bool automation)
    {
        var source = Source(automation ? AutomationCommand : SiblingReducer)
            .Replace(Body, "return new Total(Id: context.Event.Id, Amount: new OrderPlaced(context.Event.Id, context.Event.Amount).Amount);", StringComparison.Ordinal);
        if (automation) source = source.Replace("slice StateView Totals", "slice Automation Totals", StringComparison.Ordinal);
        var baseline = when_rendering_a_pure_reducer.Plan(await when_rendering_a_pure_reducer.Load(source), self: self);
        Assert.True(baseline.Success, string.Join(Environment.NewLine, baseline.Diagnostics));
        var files = when_selecting_self_reducing_rendering.Files(baseline);
        Assert.Empty(RenderedOutput.Errors(files));

        // Plant the imported command type into the real generated compilation independently of admission.
        var planted = files.Select(file => file with { Content = file.Content.Replace("RegisterAdjustment", "OrderPlaced", StringComparison.Ordinal) }).ToArray();
        var realErrors = RenderedOutput.Errors(planted);
        Assert.Equal(self, realErrors.Count > 0);
        var refused = when_rendering_a_pure_reducer.Plan(await when_rendering_a_pure_reducer.Load(source.Replace("RegisterAdjustment", "OrderPlaced", StringComparison.Ordinal)), self: self);
        Assert.Equal(!self, refused.Success);
        if (self)
        {
            Assert.Empty(refused.Artifacts);
            Assert.Contains(refused.Diagnostics, diagnostic => (diagnostic.Code == "STAGE-ESM-022" || diagnostic.Code == "STAGE-ESM-019") && diagnostic.Message.Contains("OrderPlaced", StringComparison.Ordinal));
        }
    }

    [Fact]
    public async Task should_keep_the_entire_classic_reducer_file_byte_identical()
    {
        var loaded = await when_rendering_a_pure_reducer.Load(Source(SiblingReducer));
        var plan = when_rendering_a_pure_reducer.Plan(loaded);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var file = plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("/Fold.cs", StringComparison.Ordinal));
        var content = Encoding.UTF8.GetString(file.Bytes.AsSpan());
        var descriptor = loaded.TypedContextDescriptors.Single(candidate => candidate.OperationId == loaded.Model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "Totals").ReadModels.Single(model => model.Name == "Total").Id);
        var suffix = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(descriptor.RequirementId)))[..8];
        var wrapper = SemanticTypedContextRenderer.Suffix(descriptor);
        const string modelType = "Total";

        // The reducer wrappers are not in the generated type inventory, so they keep their qualified names.
        var expected = $$"""
            using Projects.Orders.Ordering.PlaceOrder;
            using Projects.Orders.Ordering.Totals;
            using Projects.TypedContexts;

            namespace Projects.Orders.Ordering.Totals;

            public class Fold : IReducerFor<{{modelType}}>
            {
                public {{modelType}}? On(OrderPlaced @event, {{modelType}}? current, EventContext eventContext)
                {
                    var originalSourceId = eventContext.EventSourceId.Value;
                    var result = Transition_{{suffix}}(new global::Projects.TypedContexts.TypedContext_{{wrapper}}(current, @event, originalSourceId, TenantId.NotSet, eventContext.Occurred, checked((long)eventContext.SequenceNumber.Value)));
                    if (result is not null && (!string.Equals(result.Id.ToString(), originalSourceId, StringComparison.Ordinal)))
                    {
                        throw new InvalidOperationException("Reducer returned a read model with an identifier different from the event source.");
                    }
                    return result;
                }

                static {{modelType}}? Transition_{{suffix}}(global::Projects.TypedContexts.TypedContext_{{wrapper}} context)
                {
            {{Body}}
                }

            }
            """ + "\n";
        Assert.Equal(expected, content);
    }

    static string Source(string sibling = "") => when_rendering_a_pure_reducer.Source.Replace(OriginalBody, Body, StringComparison.Ordinal)
        .Replace("    slice StateView Totals", (sibling.Length == 0 ? string.Empty : Adjustment + "\n") + "    slice StateView Totals", StringComparison.Ordinal) + "\n" + sibling;

    static async Task AssertParity(string source, bool self)
    {
        var loaded = await when_rendering_a_pure_reducer.Load(source);
        var profile = self ? CratisRendering.CreateNewApplicationProfile("Projects", new("Projects", "Projects")) : CratisRendering.CreateProfile("Projects", new("Projects", "Projects"));
        var request = new ArtifactRenderRequest(loaded.Model, loaded.Plan, profile, new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id))
        {
            ImplementationRequirements = loaded.ImplementationRequirements,
            ImplementationContents = loaded.ImplementationContents,
            TypedContextDescriptors = loaded.TypedContextDescriptors
        };
        var plan = new CratisArtifactRenderPlanner().Plan(request);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var files = when_selecting_self_reducing_rendering.Files(plan);
        Assert.Empty(RenderedOutput.Errors(files));
        var compilation = RenderedOutput.CreateCompilation(files);
        var context = new SemanticApplicationContext(request, new("Projects", "Projects"));
        Assert.NotEmpty(context.Reducers);
        foreach (var reducer in context.Reducers)
        {
            var located = context.DeclaringSlice(reducer.ReadModel);
            var path = Path.Combine([.. SliceNaming.FolderPath(located.Path), self ? SliceNaming.FileName(located.Slice.Name) : $"{reducer.Name}.cs"]).Replace('\\', '/');
            foreach (var transition in reducer.Transitions)
            {
                var descriptor = loaded.TypedContextDescriptors.Single(candidate => candidate.OperationId == reducer.ReadModel && candidate.RequirementId == transition.RequirementId);
                var synthetic = PureTransitionAdmission.Analyze(loaded.ImplementationContents[transition.RequirementId], context, context.ReadModels[reducer.ReadModel], context.Events[transition.EventContract], descriptor, loaded.ImplementationRequirements.Single(requirement => requirement.RequirementId == transition.RequirementId));
                var real = PureTransitionAdmission.AnalyzeRendered(compilation, path, descriptor);
                Assert.True(synthetic.Accepted && real.Accepted, $"{synthetic.Code}: {synthetic.Reason}; {real.Code}: {real.Reason}");
                Assert.Equal(synthetic.BoundSymbols.ToArray(), real.BoundSymbols.ToArray());
                Assert.Equal(synthetic.ContextReads, real.ContextReads);
            }
        }
    }
}
#endif
