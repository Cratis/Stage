// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_generated_reducer_specifications : a_generated_application
{
    protected override ArtifactRenderPlan CreatePlan()
    {
        var source = when_rendering_a_pure_reducer.WithIdentifierConcept(when_rendering_a_pure_reducer.Source)
            .Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "context.Event.Id", StringComparison.Ordinal)
            .Replace("      event OrderPlaced\n        id OrderId\n        amount Decimal", "      event OrderPlaced\n        id OrderId\n        amount Decimal\n      specification CreatingATotal\n        when PlaceOrder\n          id = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          amount = 20\n        then OrderPlaced\n          id = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          amount = 20\n        then readmodel Total\n          id = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          amount = 20", StringComparison.Ordinal);
        source = source.Replace(
            "return new Total(context.Event.Id, context.Event.Amount);",
            "if (context.Event.Amount < 0m) return null; return new Total(context.Event.Id, (context.State?.Amount ?? 0m) + context.Event.Amount);",
            StringComparison.Ordinal)
            .Replace("      specification CreatingATotal\n", "      specification UpdatingATotalAcrossSources\n        given OrderPlaced\n          for \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          id = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          amount = 4\n        given OrderPlaced\n          for \"4fa85f64-5717-4562-b3fc-2c963f66afa7\"\n          id = \"4fa85f64-5717-4562-b3fc-2c963f66afa7\"\n          amount = 40\n        when PlaceOrder\n          id = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          amount = 5\n        then OrderPlaced\n          id = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          amount = 5\n        then readmodel Total\n          id = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          amount = 9\n      specification CreatingATotal\n", StringComparison.Ordinal);
        return when_rendering_a_pure_reducer.Plan(when_rendering_a_pure_reducer.Load(source).GetAwaiter().GetResult());
    }

    [Fact]
    async Task should_build_and_run_the_generated_reducer_specifications()
    {
        try
        {
            // ESM v3 cannot express an absent read-model expectation; prove deletion with a target-side probe.
            AddGeneratedSpecification("ReducerDeletion.cs", """
                using Cratis.Chronicle.Testing.ReadModels;
                using Projects.Common;
                using Projects.Orders.Ordering.PlaceOrder;
                using Projects.Orders.Ordering.Totals;
                using Xunit;

                namespace Projects.ReducerDeletion;

                public class when_deleting_a_total
                {
                    [Fact]
                    public async Task should_have_no_instance()
                    {
                        var key = new OrderId(Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"));
                        var scenario = new ReadModelScenario<Total>();
                        await scenario.Given.ForEventSource(key).Events(new OrderPlaced(key, 20m));
                        Assert.NotNull(scenario.InstanceForEventSourceId(key));
                        await scenario.Given.ForEventSource(key).Events(new OrderPlaced(key, -1m));
                        Assert.Null(scenario.InstanceForEventSourceId(key));
                    }

                    [Fact]
                    public async Task should_fail_the_batch_when_a_body_returns_another_sources_identifier()
                    {
                        var source = new OrderId(Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"));
                        var foreign = new OrderId(Guid.Parse("4fa85f64-5717-4562-b3fc-2c963f66afa7"));
                        var scenario = new ReadModelScenario<Total>();
                        await scenario.Given.ForEventSource(source).Events(new OrderPlaced(foreign, 20m));
                        var error = Assert.Throws<ReducerFailed>(() => scenario.InstanceForEventSourceId(source));
                        Assert.Contains("identifier different from the event source", error.Message, StringComparison.Ordinal);
                    }
                }
                """);
            var build = await Run("reducer-build.log", "build", "Projects.csproj", "-c", "Debug", "-t:Rebuild", "-warnaserror", "--nologo");
            BuildWarnings(build).ShouldEqual(string.Empty);
            var test = await Run("reducer-test.log", "test", "Projects.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo");
            Assert.Contains("Passed!", test, StringComparison.Ordinal);
        }
        finally
        {
            Cleanup();
        }
    }
}
#endif
