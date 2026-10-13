// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Runtime;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticFactAppender;

public class when_appending_a_stored_event_name : for_SemanticChronicleRegistration.given.a_stored_event_name
{
    async Task Because()
    {
        var command = _plan.Commands.Values.Single();
        var accepted = (SemanticAccepted)new SemanticEvaluator().Execute(_plan, SemanticWorld.Empty,
            SemanticExecutionRequest.Create(command.Id, [new(command.Properties.Single().Id, SemanticValue.Text("record-1"))], []));
        await new SemanticFactAppender(_client, new StageEventStoreName("Compliance"), _plan).Append(accepted.Facts, new(DateTimeOffset.UtcNow, "caller", "Caller", "user"));
    }

    [Fact] void should_append_under_the_pinned_name() => _appends.Single().Events.Single().EventType.Id.ShouldEqual("Original");
}
