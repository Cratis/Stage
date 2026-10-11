// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_generated_routed_read_model_specifications : a_generated_application
{
    internal const string Source = """
        concept OrderId : String
        concept Period : Int
        eventsource OrdersSource
          id "stored-orders"
          identifier OrderId
          stream Entries
            id "stored-entries"
            streamId Period
          stream Ignored
            id "stored-ignored"
        module Orders
          feature Ordering
            slice StateChange PlaceOrder
              command PlaceOrder
                id OrderId identifier
                amount Decimal
                period Period
                stream OrdersSource.Entries
                  streamId = period
                produces OrderPlaced
                  for id
                  amount = amount
              event OrderPlaced
                amount Decimal
              specification SummingRoutedOrders
                given OrderPlaced
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  stream OrdersSource.Entries
                    streamId = 9007199254740991
                  amount = 4
                given OrderPlaced
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  stream OrdersSource.Ignored
                  amount = 100
                given OrderPlaced
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  amount = 200
                when PlaceOrder
                  id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  amount = 5
                  period = -9007199254740991
                then OrderPlaced
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  stream OrdersSource.Entries
                    streamId = -9007199254740991
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
              query SummaryById => Summary optional
                by id OrderId
              projection Summaries => Summary
                from OrderPlaced key $eventSourceId
                  amount = amount
            slice StateView Totals
              readmodel Total
                id OrderId
                amount Decimal
              query ById => Total optional
                by id OrderId
              reducer Fold => Total
                from OrdersSource.Entries
                on OrderPlaced
                  ```csharp
                  return new Total(new(context.Key), (context.State?.Amount ?? 0m) + context.Event.Amount);
                  ```
        """;

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

                public class WrongStream : Specification, IDisposable
                {
                    readonly Cratis.Chronicle.Testing.ReadModels.ReadModelScenario<Total> _scenario = new();
                    readonly OrderId _key = new("3fa85f64-5717-4562-b3fc-2c963f66afa6");

                    async Task Establish()
                    {
                        await _scenario.Given.ForEventSource(_key).OnStream("stored-orders", "stored-entries", "9007199254740991").Events(new OrderPlaced(4m));
                        await _scenario.Given.ForEventSource(_key).OnStream("stored-orders", "stored-ignored").Events(new OrderPlaced(5m));
                    }

                    [Fact] void should_not_accept_an_expectation_that_counts_the_wrong_stream() => _scenario.InstanceForEventSourceId(_key)!.Amount.ShouldEqual(9m);

                    public void Dispose() => _scenario.Dispose();
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
