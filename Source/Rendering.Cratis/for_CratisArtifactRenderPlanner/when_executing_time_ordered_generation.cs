// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_time_ordered_generation : a_generated_application
{
    const string Probe = """
        #if DEBUG
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Chronicle.EventSequences;
        using GenerationApp.Common;
        using GenerationApp.Orders.Placement.Place;
        using Microsoft.Extensions.DependencyInjection;
        using Xunit;

        namespace GenerationApp.ReceiptProbe;

        public class when_generating_at_dispatch
        {
            static readonly DateTimeOffset Receipt = new(2027, 3, 4, 10, 0, 0, 123, TimeSpan.Zero);

            [Fact]
            public async Task should_encode_the_pipeline_receipt_in_both_time_ordered_values()
            {
                using var scenario = new CommandScenario<PlaceOrder>();
                var clock = new FixedClock();
                scenario.Services.AddSingleton<TimeProvider>(clock);
                scenario.AllowGeneration();
                using var principalScope1 = new global::Cratis.Arc.Authorization.CurrentPrincipalAccessor(new global::Cratis.Arc.Http.HttpRequestContextAccessor()).BeginScope(new global::System.Security.Claims.ClaimsPrincipal(new global::System.Security.Claims.ClaimsIdentity([], "Probe")));
                var result = await scenario.Execute(new PlaceOrder());
                Assert.True(result.IsSuccess, string.Join("; ", result.ExceptionMessages));
                Assert.Single(scenario.AppendedEvents);
                var stored = Assert.Single(await scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First));
                var payload = Assert.IsType<OrderPlaced>(stored.Content);
                var typeIdTime = Decode(scenario.GeneratedValues.Last<OrderId>("orderId").Value[6..16]);
                var ulidTime = Decode(scenario.GeneratedValues.Last<ReceiptId>("receiptId").Value[..10]);
                Assert.Equal(Receipt.ToUnixTimeMilliseconds(), typeIdTime);
                Assert.Equal(Receipt.ToUnixTimeMilliseconds(), ulidTime);
                Assert.Equal(payload.PlacedAt.ToUnixTimeMilliseconds(), typeIdTime);
                Assert.Equal(payload.PlacedAt.ToUnixTimeMilliseconds(), ulidTime);
                Assert.Equal(Receipt, payload.PlacedAt);
                Assert.Equal(1, clock.Calls);
            }

            [Fact]
            public async Task should_use_the_scenario_clock_for_unpinned_generation()
            {
                using var scenario = new CommandScenario<PlaceOrder>();
                scenario.Clock(Receipt);
                scenario.AllowGeneration();
                using var principalScope2 = new global::Cratis.Arc.Authorization.CurrentPrincipalAccessor(new global::Cratis.Arc.Http.HttpRequestContextAccessor()).BeginScope(new global::System.Security.Claims.ClaimsPrincipal(new global::System.Security.Claims.ClaimsIdentity([], "Probe")));
                var result = await scenario.Execute(new PlaceOrder());
                Assert.True(result.IsSuccess, string.Join("; ", result.ExceptionMessages));
                Assert.Equal(Receipt.ToUnixTimeMilliseconds(), Decode(scenario.GeneratedValues.Last<OrderId>("orderId").Value[6..16]));
                Assert.Equal(Receipt.ToUnixTimeMilliseconds(), Decode(scenario.GeneratedValues.Last<ReceiptId>("receiptId").Value[..10]));
            }

            static long Decode(string time) => time.Aggregate(0L, (value, symbol) => (value << 5) + "0123456789ABCDEFGHJKMNPQRSTVWXYZ".IndexOf(char.ToUpperInvariant(symbol)));

            sealed class FixedClock : TimeProvider
            {
                public int Calls { get; private set; }
                public override DateTimeOffset GetUtcNow()
                {
                    Calls++;
                    return Receipt;
                }
            }
        }
        #endif
        """;

    string _debug = null!;
    string _tests = null!;

    protected override ArtifactRenderPlan CreatePlan()
    {
        var model = invoice_model.Compile("""
            concept OrderId : String
              generate typeid "order"
            concept ReceiptId : String
              generate ulid
            module Orders
              feature Placement
                slice StateChange Place
                  command PlaceOrder
                    orderId OrderId generated identifier
                    receiptId ReceiptId generated
                    produces event OrderPlaced
                      orderId OrderId = orderId
                      receiptId ReceiptId = receiptId
                      placedAt DateTime = $context.occurred
                    returns orderId
            """);

        return CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("GenerationApp", "GenerationApp"));
    }

    async Task Because()
    {
        AddGeneratedSpecification("ReceiptProbe.cs", Probe);
        _debug = await Run("time-ordered-debug.log", "build", "-c", "Debug", "-warnaserror");
        _tests = await Run("time-ordered-tests.log", "test", "-c", "Debug", "--no-build");
    }

    [Fact] void should_execute_the_time_ordered_generation_contract()
    {
        BuildWarnings(_debug).ShouldEqual(string.Empty);
        _tests.ShouldContain("Passed!");
        ReadGeneratedFile("Common/OrderId.cs").ShouldNotContain("UtcNow");
        ReadGeneratedFile("Common/ReceiptId.cs").ShouldNotContain("UtcNow");
    }
}
#endif
