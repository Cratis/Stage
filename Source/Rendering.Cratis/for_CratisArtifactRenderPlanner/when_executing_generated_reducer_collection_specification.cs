// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_generated_reducer_collection_specification : a_generated_application
{
    protected override ArtifactRenderPlan CreatePlan() => CreateCollectionPlan(optional: false);

    internal static ArtifactRenderPlan CreateCollectionPlan(bool optional)
    {
        const string originalBody = "return new Total(Guid.Parse(\"00000000-0000-0000-0000-000000000001\"), context.Event.Amount);";
        var source = when_rendering_a_pure_reducer.WithIdentifierConcept(when_rendering_a_pure_reducer.Source)
            .Replace("      readmodel Total\n        id OrderId\n        amount Decimal", "      readmodel Total\n        id OrderId\n        amount Decimal\n        amounts Decimal[]", StringComparison.Ordinal)
            .Replace("      event OrderPlaced\n        id OrderId\n        amount Decimal", "      event OrderPlaced\n        id OrderId\n        amount Decimal\n      specification WithAmounts\n        when PlaceOrder\n          id = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          amount = 20\n        then OrderPlaced\n          id = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          amount = 20\n        then readmodel Total\n          id = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          amounts = [20, 40]", StringComparison.Ordinal);
        if (optional)
        {
            source = source.Replace("amounts Decimal[]", "amounts Decimal[]?", StringComparison.Ordinal);
        }

        var loaded = when_rendering_a_pure_reducer.Load(source.Replace(originalBody, "return context.State;", StringComparison.Ordinal)).GetAwaiter().GetResult();
        var readModel = loaded.Model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.ReadModels.Length > 0).ReadModels.Single();
        var expressions = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["id"] = "context.Event.Id",
            ["amount"] = "context.Event.Amount",
            ["amounts"] = "global::System.Collections.Immutable.ImmutableArray.ToImmutableArray(amounts)"
        };
        var arguments = string.Join(", ", SemanticStateViewArtifactRenderer.OrderedProperties(readModel.Properties).Select(property => expressions[property.Name]));
        var body = $"global::System.Collections.Generic.IEnumerable<decimal> amounts = new decimal[] {{ context.Event.Amount, 40m }}; return new Total({arguments});";
        var plan = when_rendering_a_pure_reducer.Plan(when_rendering_a_pure_reducer.Load(source.Replace(originalBody, body, StringComparison.Ordinal)).GetAwaiter().GetResult());
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var spec = plan.Artifacts.Single(artifact => artifact.RelativePath.Contains("with_amounts_is_projected", StringComparison.Ordinal));
        var content = System.Text.Encoding.UTF8.GetString(spec.Bytes.AsSpan());
        Assert.Contains("SequenceEqual(", content, StringComparison.Ordinal);
        Assert.Contains("static readonly decimal[] _expected_amounts = [20m, 40m]", content, StringComparison.Ordinal);
        return plan;
    }

    [Fact]
    async Task should_build_and_pass_the_nonempty_collection_expectation()
    {
        try
        {
            var build = await Run("reducer-collection-build.log", "build", "Projects.csproj", "-c", "Debug", "-t:Rebuild", "-warnaserror", "--nologo");
            BuildWarnings(build).ShouldEqual(string.Empty);
            var test = await Run("reducer-collection-test.log", "test", "Projects.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo");
            Assert.Contains("Passed!", test, StringComparison.Ordinal);
        }
        finally
        {
            Cleanup();
        }
    }
}
#endif
