// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_checking_reducer_layout_purity
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_bind_concepts_immutable_event_and_state_collections_and_context_identically(bool self)
    {
        const string originalBody = "return new Total(Guid.Parse(\"00000000-0000-0000-0000-000000000001\"), context.Event.Amount);";
        var source = when_rendering_a_pure_reducer.WithIdentifierConcept(when_rendering_a_pure_reducer.Source)
            .Replace("        amount Decimal\n        produces", "        amount Decimal\n        amounts Decimal[]\n        produces", StringComparison.Ordinal)
            .Replace("          amount = amount\n      event", "          amount = amount\n          amounts = amounts\n      event", StringComparison.Ordinal)
            .Replace("      event OrderPlaced\n        id OrderId\n        amount Decimal", "      event OrderPlaced\n        id OrderId\n        amount Decimal\n        amounts Decimal[]", StringComparison.Ordinal)
            .Replace("      readmodel Total\n        id OrderId\n        amount Decimal", "      readmodel Total\n        id OrderId\n        amount Decimal\n        amounts Decimal[]", StringComparison.Ordinal);
        var initial = await when_rendering_a_pure_reducer.Load(source.Replace(originalBody, "return context.State;", StringComparison.Ordinal));
        var model = initial.Model.Application.Modules.Single().Features.Single().Slices.Single(slice => !slice.Reducers.IsEmpty).ReadModels.Single();
        var expressions = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["id"] = "context.Event.Id",
            ["amount"] = "context.Tenant == TenantId.Default ? context.SequenceNumber : context.Occurred.Day",
            ["amounts"] = "context.State?.Amounts ?? context.Event.Amounts"
        };
        var body = $"return new Total({string.Join(", ", SemanticStateViewArtifactRenderer.OrderedProperties(model.Properties).Select(property => expressions[property.Name]))});";
        var loaded = await when_rendering_a_pure_reducer.Load(source.Replace(originalBody, body, StringComparison.Ordinal));
        var profile = self ? CratisRendering.CreateNewApplicationProfile("Projects", new("Projects", "Projects")) : CratisRendering.CreateProfile("Projects", new("Projects", "Projects"));
        var request = new ArtifactRenderRequest(loaded.Model, loaded.Plan, profile, new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id))
        {
            ImplementationRequirements = loaded.ImplementationRequirements,
            ImplementationContents = loaded.ImplementationContents,
            TypedContextDescriptors = loaded.TypedContextDescriptors
        };
        var context = new SemanticApplicationContext(request, new("Projects", "Projects"));
        var reducer = Assert.Single(context.Reducers);
        var descriptor = Assert.Single(loaded.TypedContextDescriptors);
        var transition = Assert.Single(reducer.Transitions);
        var synthetic = PureTransitionAdmission.Analyze(body, context, context.ReadModels[reducer.ReadModel], context.Events[transition.EventContract], descriptor, Assert.Single(loaded.ImplementationRequirements));
        Assert.True(synthetic.Accepted, synthetic.Reason);
        var plan = new CratisArtifactRenderPlanner().Plan(request);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var compilation = RenderedOutput.CreateCompilation(when_selecting_self_reducing_rendering.Files(plan));
        var file = plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith(self ? "Totals.cs" : "Fold.cs", StringComparison.Ordinal));
        var real = PureTransitionAdmission.AnalyzeRendered(compilation, file.RelativePath, descriptor);
        Assert.True(real.Accepted, real.Reason);
        Assert.Equal(synthetic.BoundSymbols.ToArray(), real.BoundSymbols.ToArray());
        Assert.Equal(synthetic.ContextReads, real.ContextReads);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_never_admit_calls_to_generated_query_methods_as_pure(bool self)
    {
        var source = when_rendering_a_pure_reducer.Source.Replace("return new Total(Guid.Parse(\"00000000-0000-0000-0000-000000000001\"), context.Event.Amount);", "_ = Total.ById(null!, context.Event.Id); return context.State;", StringComparison.Ordinal);
        var plan = when_rendering_a_pure_reducer.Plan(await when_rendering_a_pure_reducer.Load(source), self: self);
        given.rendering_refusals.OmittedParts(plan);
        Assert.Contains(plan.Diagnostics, diagnostic => string.Equals(diagnostic.Code, "STAGE-ESM-019", StringComparison.Ordinal) || string.Equals(diagnostic.Code, "STAGE-ESM-022", StringComparison.Ordinal));
    }
}
#endif
