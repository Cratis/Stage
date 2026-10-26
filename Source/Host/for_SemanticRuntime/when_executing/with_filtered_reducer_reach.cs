// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class with_filtered_reducer_reach : Specification
{
    [Theory]
    [InlineData("", false, false)]
    [InlineData("Account.Archive", false, false)]
    [InlineData("Other.Notes", false, false)]
    [InlineData("Account.Notes", true, false)]
    [InlineData("Account.Notes", true, true)]
    public async Task should_defer_reducer_reach_until_the_concrete_route(string route, bool matching, bool constrained)
    {
        var source = $$"""
            eventsource Account
              stream Notes
              stream Archive
            eventsource Other
              stream Notes
            module Work
              feature Items
                slice StateChange Manage
                  event Changed
                  command Feed
                    id String identifier
            {{(route.Length == 0 ? "" : "        stream " + route)}}
                    produces Changed
                      for id
            {{(constrained ? "        produces Changed\n          for id" : "")}}
                  readmodel Snapshot
                    id String key
                  reducer Fold => Snapshot
                    from Account.Notes
                    on Changed
                      ```csharp
                      throw new InvalidOperationException("Never execute an authored body");
                      ```
            {{(constrained ? "      constraint Once\n        unique event Changed\n        message \"Already changed\"" : "")}}
            """;
        var plan = compiled_plan.From(source);
        new SemanticRuntimeAdmission(plan).Blocking.ShouldBeEmpty();
        var appender = Substitute.For<IAppendSemanticFacts, IAppendSemanticFactsAtTail, ISemanticFactTail>();
        ((ISemanticFactTail)appender).Tail().Returns(ulong.MaxValue);
        var runtime = SemanticRuntimeHosting.Create(plan, appender);
        using var lifetime = (IDisposable)runtime;
        var result = await runtime.Execute(plan.Commands.Values.Single(), new Dictionary<string, JsonElement> { ["id"] = JsonSerializer.SerializeToElement("one") }, new ClaimsPrincipal(), new(DateTimeOffset.UnixEpoch, "owner", "Owner", "owner"), false);
        if (matching)
        {
            if (constrained)
            {
                ((SemanticRejected)result).Category.ShouldEqual(SemanticRejectionCategory.Constraint);
                ((SemanticRejected)result).Code.ShouldEqual("Once");
            }
            else ((SemanticUnsupported)result).Capability.ShouldEqual(SemanticExecutionCapability.Projection);
            result.World.Facts.ShouldBeEmpty();
            await ((IAppendSemanticFactsAtTail)appender).DidNotReceive().Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>(), Arg.Any<ulong>());
        }
        else
        {
            result.ShouldBeOfExactType<SemanticAccepted>();
            result.World.Facts.Length.ShouldEqual(1);
            await ((IAppendSemanticFactsAtTail)appender).Received(1).Append(Arg.Is<IReadOnlyList<SemanticFact>>(facts => facts.Count == 1), Arg.Any<SemanticCommandOccurrence>(), ulong.MaxValue);
        }
        ((ISemanticRuntimeStatus)runtime).FaultReason.ShouldBeNull();
    }
}
