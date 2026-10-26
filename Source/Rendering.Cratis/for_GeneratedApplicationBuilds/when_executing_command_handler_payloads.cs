// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_body_carrying_command_handlers;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationBuilds;

public class when_executing_command_handler_payloads : a_generated_application
{
    string _commandNamespace = null!;
    string _build = null!;
    string _test = null!;

    protected override ArtifactRenderPlan CreatePlan()
    {
        var source = "concept EventForEventSourceId : String\nconcept OrderId : Uuid\n  generate uuid\n" + handler_model.MultiSource +
            string.Concat(Enumerable.Repeat("\n        produces OrderPlaced\n          for orderId\n          name = name", 7)) + "\n" + """
                  command FastPlace
                    orderId Uuid identifier
                    name String
                    marker EventForEventSourceId
                    handler
                      implementation
                        hint "Uppercase the name"
                    produces OrderPlaced
                      for orderId
                      name = name
                  command GeneratedPlace
                    orderId OrderId generated identifier
                    quantity Int
                    handler
                      implementation
                        hint "Adjust the quantity"
                    produces event OrderRegistered
                      for orderId
                      orderId OrderId = orderId
                      quantity Int = quantity
            """;
        var tupleBody = handler_model.MultiBody.Replace("new OrderEchoed(name));", "new OrderEchoed(name), " + string.Join(", ", Enumerable.Range(3, 7).Select(index => $"new OrderPlaced(name + \"{index}\")")) + ");", StringComparison.Ordinal);
        var request = handler_model.Request(source: source, bodies: new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["PlaceOrder"] = tupleBody,
            ["FastPlace"] = "return new OrderPlaced(context.Command.Name.ToUpperInvariant() + context.Command.Marker.Value);",
            ["GeneratedPlace"] = "global::System.AppContext.SetData(context.Command.OrderId.Value.ToString(), 1 + ((int?)global::System.AppContext.GetData(context.Command.OrderId.Value.ToString()) ?? 0)); return new OrderRegistered(context.Command.OrderId, context.Command.Quantity + 10);"
        });
        var context = new SemanticApplicationContext(request, new("HandlerApp", "Orders"));
        var command = context.Commands.Values.First();
        _commandNamespace = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(command.Id).Path);
        return handler_model.Plan(request);
    }

    void Establish() => AddGeneratedSpecification("handler_payloads.cs", $$"""
        using System;
        using System.Linq;
        using Xunit;
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Chronicle.EventSequences;
        using {{_commandNamespace}};
        using Orders.Common;

        public class handler_payloads
        {
            [Fact]
            public void uses_one_typed_payload_batch_and_the_filled_generated_value()
            {
                var id = Guid.NewGuid();
                var token = new ReceiptToken(Guid.NewGuid());
                var key = Guid.NewGuid().ToString();
                var facts = new PlaceOrder(id, key).Handle(token).ToArray();
                Assert.Equal(9, facts.Length);
                var placed = Assert.IsType<OrderPlaced>(facts[0].Event);
                var echoed = Assert.IsType<OrderEchoed>(facts[1].Event);
                Assert.Equal(placed.Name, echoed.Name);
                Assert.StartsWith(key + token.Value.ToString(), placed.Name);
                Assert.Equal(1, (int)AppContext.GetData("S1:" + key)!);
                Assert.Equal(facts[0].EventSourceId, facts[1].EventSourceId);
            }

            [Fact]
            public async System.Threading.Tasks.Task generates_once_and_invokes_the_helper_once_through_arc()
            {
                using var scenario = new CommandScenario<PlaceOrder>();
                scenario.AllowGeneration();
                var key = Guid.NewGuid().ToString();
                var result = await scenario.Execute(new PlaceOrder(Guid.NewGuid(), key));
                Assert.True(result.IsSuccess, string.Join("; ", result.ExceptionMessages));
                var token = Assert.Single(scenario.GeneratedValues.All<ReceiptToken>("token"));
                var stored = (await scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).ToArray();
                Assert.Equal(9, stored.Length);
                Assert.Equal(key + token.Value.ToString(), Assert.IsType<OrderPlaced>(stored[0].Content).Name);
                Assert.Equal(key + token.Value.ToString(), Assert.IsType<OrderEchoed>(stored[1].Content).Name);
                Assert.Equal(1, (int)AppContext.GetData("S1:" + key)!);
                for (var index = 2; index < stored.Length; index++)
                {
                    Assert.Equal(key + token.Value.ToString() + (index + 1).ToString(), Assert.IsType<OrderPlaced>(stored[index].Content).Name);
                }
            }

            [Fact]
            public void executes_the_fast_path_instead_of_the_misleading_mapping()
            {
                var payload = new FastPlace(Guid.NewGuid(), "alice", new global::Orders.Common.EventForEventSourceId("!")).Handle();
                Assert.Equal("ALICE!", payload.Name);
            }

            [Fact]
            public async System.Threading.Tasks.Task sees_a_generated_identifier_once_and_ignores_the_quantity_mapping()
            {
                using var scenario = new CommandScenario<GeneratedPlace>();
                scenario.AllowGeneration();
                var result = await scenario.Execute(new GeneratedPlace(2));
                Assert.True(result.IsSuccess, string.Join("; ", result.ExceptionMessages));
                var id = Assert.Single(scenario.GeneratedValues.All<OrderId>("orderId"));
                var stored = Assert.Single(await scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First));
                var payload = Assert.IsType<OrderRegistered>(stored.Content);
                Assert.Equal(id, payload.OrderId);
                Assert.Equal(12, payload.Quantity);
                Assert.Equal(1, (int)AppContext.GetData(id.Value.ToString())!);
            }

            [Fact]
            public async System.Threading.Tasks.Task does_not_reach_the_body_when_validation_fails()
            {
                using var scenario = new CommandScenario<PlaceOrder>();
                scenario.AllowGeneration();
                var result = await scenario.Execute(new PlaceOrder(Guid.NewGuid(), ""));
                Assert.False(result.IsSuccess);
                Assert.Empty(scenario.AppendedEvents);
                Assert.Null(AppContext.GetData("S1:"));
            }
        }
        """);

    async Task Because()
    {
        _build = await Run("command-handlers-build.log", "build", "HandlerApp.csproj", "-c", "Debug", "-warnaserror", "--nologo");
        _test = await Run("command-handlers-test.log", "test", "HandlerApp.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo");
    }

    [Fact]
    public void should_build_and_execute_the_generated_body_with_typed_tuple_slots()
    {
        Assert.Equal(string.Empty, BuildWarnings(_build));
        Assert.Contains("Passed!", _test, StringComparison.Ordinal);
    }
}
#endif
