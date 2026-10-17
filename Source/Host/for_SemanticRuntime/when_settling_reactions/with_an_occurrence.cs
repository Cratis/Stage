// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_settling_reactions;

public class with_an_occurrence : a_reaction_runtime
{
    protected override string Model => StartSource + """

            slice Automation FollowUp
              command Record
                id String identifier
                seen DateTime
                produces Completed
                  for id
                  seen = seen
                  stamped = $context.occurred
              event Completed
                seen DateTime
                stamped DateTime
              event Finished
                finishedAt DateTime
              reaction Worker
                runs as system
                when Started
                  invokes Record
                    id = id
                    seen = $context.occurred
              reaction Finisher
                when Completed
                  produces Finished
                    finishedAt = $context.occurred
        """;

    async Task Because() => _result = await _runtime.Execute(
        _plan.Commands.Values.Single(command => command.Name == "StartWork"),
        new Dictionary<string, JsonElement>
        {
            ["id"] = JsonSerializer.SerializeToElement("one"),
            ["amount"] = JsonSerializer.SerializeToElement(7)
        },
        new ClaimsPrincipal(),
        new(new DateTimeOffset(2027, 3, 4, 10, 0, 0, 123, TimeSpan.FromHours(2)), "subject", "name", "user"),
        false);

    [Fact] void should_admit_occurrence_contexts() => SemanticRunAdmission.ModelFeatures(_plan.Model).ShouldBeEmpty();
    [Fact] void should_settle_the_invocation_and_cascade() => FactNames(((SemanticAccepted)_result).Facts).ToArray().ShouldEqual(["Started", "Completed", "Finished"]);
    [Fact] void should_map_exact_utc_values() => ((SemanticAccepted)_result).Facts.Skip(1).SelectMany(fact => fact.Values).Select(value => value.Value).ShouldEachConformTo(value => value == SemanticValue.Text("2027-03-04T08:00:00.1230000Z"));
    [Fact] void should_commit_all_three_facts_together() => _batches.Single().Count.ShouldEqual(3);
    [Fact] void should_preserve_occurrence_in_the_committed_batch() => _batches.Single().All(fact => fact.Occurred == new DateTimeOffset(2027, 3, 4, 8, 0, 0, 123, TimeSpan.Zero)).ShouldBeTrue();
}
