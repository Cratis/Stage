// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_generated_routed_read_model_specifications : a_generated_application
{
    internal static string Source => when_rendering_a_pure_reducer.WithIdentifierConcept(when_rendering_reducer_observer_filters.Source("OrdersSource.Entries"))
        .Replace("id \"stored-entries\"", "id \"stored-entries\"\n    streamId Int", StringComparison.Ordinal)
        .Replace("        amount Decimal\n        produces", "        amount Decimal\n        period Int\n        stream OrdersSource.Entries\n          streamId = period\n        produces", StringComparison.Ordinal)
        .Replace("return new Total(context.Event.Id, context.Event.Amount);", "return new Total(context.Event.Id, (context.State?.Amount ?? 0m) + context.Event.Amount);", StringComparison.Ordinal)
        .Replace(
            "    slice StateView Totals",
            """
              specification SummingRoutedOrders
                given OrderPlaced
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  stream OrdersSource.Entries
                    streamId = 9007199254740991
                  id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  amount = 4
                given OrderPlaced
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  stream OrdersSource.Ignored
                  id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  amount = 100
                given OrderPlaced
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  amount = 200
                when PlaceOrder
                  id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  amount = 5
                  period = -9007199254740991
                then OrderPlaced
                  id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  amount = 5
                then readmodel Total
                  id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  amount = 9
                then readmodel Summary
                  id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  amount = 5
                then query SummaryById
                  arguments
                    id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  result
                    id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                    amount = 5
            slice StateView Summaries
              readmodel Summary
                id OrderId
                amount Decimal
              query SummaryById => Summary?
                by id OrderId
              projection Summaries => Summary
                from OrderPlaced key id
                  id = id
                  amount = amount
            slice StateView Totals
        """,
            StringComparison.Ordinal);

    protected override ArtifactRenderPlan CreatePlan() => when_rendering_a_pure_reducer.Plan(when_rendering_a_pure_reducer.Load(Source).GetAwaiter().GetResult());

    [Fact]
    async Task should_run_routed_replay_and_detect_a_wrong_stream()
    {
        try
        {
            AddGeneratedSpecification("WrongStream.cs", """
                using Cratis.Specifications;
                using Projects.Common;
                using Projects.Orders.Ordering.PlaceOrder;
                using Projects.Orders.Ordering.Totals;
                using Xunit;

                namespace Projects.WrongStream;

                public class WrongStream : Specification
                {
                    readonly Cratis.Chronicle.Testing.ReadModels.ReadModelScenario<Total> _scenario = new();
                    readonly OrderId _key = new(Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"));

                    async Task Establish()
                    {
                        await _scenario.Given.ForEventSource(_key).OnStream("stored-orders", "stored-entries").Events(new OrderPlaced(_key, 4m));
                        await _scenario.Given.ForEventSource(_key).OnStream("stored-orders", "stored-ignored").Events(new OrderPlaced(_key, 5m));
                    }

                    [Fact] void should_not_accept_an_expectation_that_counts_the_wrong_stream() => _scenario.InstanceForEventSourceId(_key)!.Amount.ShouldEqual(9m);
                }
                """);
            var build = await Run("routed-read-model-build.log", "build", "Projects.csproj", "-c", "Debug", "-warnaserror", "--nologo");
            Assert.Equal(string.Empty, BuildWarnings(build));
            var passing = await Run("routed-read-model-test.log", "test", "Projects.csproj", "-c", "Debug", "--no-build", "--no-restore", "--filter", "FullyQualifiedName!~Projects.WrongStream", "--nologo");
            Assert.Contains("Passed!", passing, StringComparison.Ordinal);
            var failing = await Run("routed-read-model-wrong-stream.log", true, "test", "Projects.csproj", "-c", "Debug", "--no-build", "--no-restore", "--filter", "FullyQualifiedName~Projects.WrongStream", "--nologo");
            Assert.Contains("Failed!", failing, StringComparison.Ordinal);
            Assert.Contains("Failed:     1", failing, StringComparison.Ordinal);
        }
        finally
        {
            Cleanup();
        }
    }
}
#endif
