// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing.given;

public class a_whole_number_route : Specification
{
    const string Source = """
        concept Count : Int
        eventsource Stock
          identifier String
          stream Entries
            streamId Count
        module Inventory
          feature Stock
            slice StateChange Record
              command RecordStock
                id String identifier
                count Count
                stream Stock.Entries
                  streamId = count
                produces StockRecorded
                  for id
                  count = count
              event StockRecorded
                count Count
              specification RecordingPositive
                when RecordStock
                  id = "positive"
                  count = 900719925474099
                then StockRecorded
                  stream Stock.Entries
                    streamId = 900719925474099
                  count = 900719925474099
              specification RecordingNegative
                when RecordStock
                  id = "negative"
                  count = -900719925474099
                then StockRecorded
                  stream Stock.Entries
                    streamId = -900719925474099
                  count = -900719925474099
        """;

    protected SemanticFact? _appended;
    protected SemanticExecutionResult _result = null!;
    SemanticExecutionPlan _plan = null!;
    IAppendSemanticFacts _appender = null!;

    void Establish()
    {
        _plan = compiled_plan.From(Source);
        _appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        ((ISemanticFactTail)_appender).Tail().Returns(ulong.MaxValue);
        _appender.Append(Arg.Do<IReadOnlyList<SemanticFact>>(facts => _appended = facts.Single()), Arg.Any<SemanticCommandOccurrence>()).Returns(Task.CompletedTask);
    }

    protected async Task Record(long count)
    {
        var runtime = SemanticRuntimeHosting.Create(_plan, _appender);
        _result = await runtime.Execute(
            _plan.Commands.Values.Single(),
            new Dictionary<string, JsonElement>
            {
                ["id"] = JsonSerializer.SerializeToElement("stock"),
                ["count"] = JsonSerializer.SerializeToElement(count)
            },
            new ClaimsPrincipal(),
            new(DateTimeOffset.UtcNow, "subject", "name", "user"),
            false);
    }
}
