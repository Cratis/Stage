// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime;

public class when_executing_command_validation_bodies : Specification
{
    [Fact]
    async Task should_accept_and_reject_in_live_execution()
    {
        var directory = Directory.CreateTempSubdirectory("stage-command-validation-").FullName;
        try
        {
            var path = Path.Combine(directory, "Model.play");
            await File.WriteAllTextAsync(path, Model);
            var loaded = await SemanticModelLoader.LoadFromPathAsync(path);
            var notes = new List<SemanticModelLoadDiagnostic>();
            await using var bodies = await SemanticBodyWorkerHosting.Start(
                loaded,
                path,
                new(true, Path.Combine(AppContext.BaseDirectory, "bodyworker", "Cratis.Stage.BodyWorker.dll"))
                {
                    CacheDirectory = Path.Combine(directory, "cache"), CallTimeoutMilliseconds = 30000
                },
                notes);
            Assert.NotNull(bodies);
            Assert.All(bodies.Manifest.Entries, entry => Assert.True(entry.Admitted, entry.DiagnosticMessage));
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
            var accepted = await Execute(runtime, loaded, 1);
            Assert.True(accepted is SemanticAccepted, accepted.ToString());
            var rejected = await Execute(runtime, loaded, 0);
            Assert.True(rejected is SemanticRejected, rejected.ToString());
            ((SemanticRejected)rejected).Details.ShouldEqual("Positive amount required");
            await appender.Received(1).Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>());
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    static Task<SemanticExecutionResult> Execute(ISemanticRuntime runtime, LoadedSemanticModel loaded, decimal amount) => runtime.Execute(
        loaded.Plan.Commands.Values.Single(),
        new Dictionary<string, JsonElement>
        {
            ["orderId"] = JsonSerializer.SerializeToElement("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
            ["amount"] = JsonSerializer.SerializeToElement(amount)
        },
        new ClaimsPrincipal(),
        new(DateTimeOffset.UnixEpoch, "owner", "Owner", "owner"),
        false);

    internal const string Model = """
        concept OrderId : Uuid
        module Orders
          feature Ordering
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId OrderId identifier
                amount Decimal
                validate csharp
                  ```csharp
                  if (context.Artifact.Amount <= 0) yield return "Positive amount required";
                  ```
                produces OrderPlaced
                  for orderId
                  amount = amount
              event OrderPlaced
                amount Decimal
              specification Accepted
                when PlaceOrder
                  orderId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  amount = 1
                then OrderPlaced
                  amount = 1
              specification Rejected
                when PlaceOrder
                  orderId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  amount = 0
                then error "Positive amount required"
        """;
}
