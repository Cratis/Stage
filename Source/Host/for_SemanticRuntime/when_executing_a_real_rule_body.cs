// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime;

public class when_executing_a_real_rule_body : Specification
{
    [Theory]
    [InlineData("amount", "amount")]
    [InlineData("placeOrder", "placeOrder_")]
    public async Task should_accept_and_reject_using_the_rendered_worker_rule(string property, string wireProperty)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"stage-runtime-worker-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "Model.play");
            await File.WriteAllTextAsync(path, Model(property, wireProperty));
            var loaded = await SemanticModelLoader.LoadFromPathAsync(path);
            var notes = new List<SemanticModelLoadDiagnostic>();
            var settings = new SemanticBodyWorkerOptions(true, Path.Combine(AppContext.BaseDirectory, "bodyworker", "Cratis.Stage.BodyWorker.dll"))
            {
                CacheDirectory = Path.Combine(directory, "cache"),
                CallTimeoutMilliseconds = 10000
            };
            var bodies = await SemanticBodyWorkerHosting.Start(loaded, path, settings, notes);
            Assert.True(bodies is not null, string.Join(';', notes.Select(note => note.Message)));
            var appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
            var tail = ulong.MaxValue;
            ((ISemanticFactTail)appender).Tail().Returns(_ => tail);
            appender.Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>()).Returns(_ =>
            {
                tail = 0;
                return Task.CompletedTask;
            });
            var runtime = SemanticRuntimeHosting.Create(loaded.Plan, appender, SemanticWorld.Empty, bodies);
            using var lifetime = (IDisposable)runtime;
            foreach (var amount in new[] { 5, -5 })
            {
                var payload = new Dictionary<string, JsonElement>
                {
                    ["orderId"] = JsonSerializer.SerializeToElement("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
                    [property] = JsonSerializer.SerializeToElement(amount)
                };
                var result = await runtime.Execute(loaded.Plan.Commands.Values.Single(), payload, new ClaimsPrincipal(), new(DateTimeOffset.UnixEpoch, "owner", "Owner", "owner"), false);
                if (amount > 0) Assert.IsType<SemanticAccepted>(result);
                else Assert.IsType<SemanticRejected>(result).Details.ShouldEqual("Positive amount required");
            }
            await appender.Received(1).Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>());
            AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name?.StartsWith("BodyUnit_", StringComparison.Ordinal) == true).ShouldBeFalse();
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    static string Model(string property, string wireProperty) => $$"""
        concept OrderId : Uuid
        module Orders
          feature Ordering
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId OrderId identifier
                {{property}} Decimal
                validate
                  {{property}} rule Positive message "Positive amount required"
                    ```csharp
                    return context.Property == "{{wireProperty}}" && context.Value > 0;
                    ```
                produces OrderPlaced
                  for orderId
                  {{property}} = {{property}}
              event OrderPlaced
                {{property}} Decimal
        """;
}
