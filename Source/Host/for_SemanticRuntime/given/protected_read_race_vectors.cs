// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.given;

internal static class protected_read_race_vectors
{
    internal static SemanticFact Fact(SemanticExecutionPlan plan, JsonNode json)
    {
        var contract = plan.Events.Values.Single(value => value.Name == json["event"]!.GetValue<string>());
        var source = SemanticValue.Text(json["source"]!.GetValue<string>());
        var type = json["sourceType"] is { } concept
            ? SemanticTypeReference.ForConcept(plan.Model.Application.Concepts.Single(value => value.Name == concept.GetValue<string>()).Id)
            : SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text);
        var values = contract.Properties.Select(property => new SemanticPropertyValue(property.Id, SemanticValue.Text(json["values"]![property.Name]!.GetValue<string>())));

        return new(contract.Id, source, [.. values]) { Context = new(new(type, source)) };
    }

    internal static SemanticDecision Decide(SemanticExecutionPlan plan, SemanticWorld world, JsonNode json)
    {
        var command = plan.Commands.Values.Single(value => value.Name == json["command"]!.GetValue<string>());
        var request = SemanticExecutionRequest.Create(command.Id, Values(command, json["values"]!.AsObject()), []);
        if (json["generated"] is JsonObject generated)
        {
            request = request with { GeneratedValues = Values(command, generated) };
        }

        return new SemanticEvaluator().Decide(plan, world, request);
    }

    internal static JsonObject Outcome(SemanticExecutionPlan plan, SemanticExecutionResult result)
    {
        var outcome = result switch
        {
            SemanticAccepted accepted => new JsonObject { ["outcome"] = "accepted", ["facts"] = new JsonArray([.. accepted.Facts.Select(fact => (JsonNode?)Observed(plan, fact))]) },
            SemanticConflict conflict => new JsonObject { ["outcome"] = "conflict", ["category"] = conflict.Category.ToString() },
            SemanticRejected rejected => Rejected(rejected),
            _ => throw new InvalidSemanticContract($"Unexpected race outcome: {result}")
        };
        outcome["world"] = new JsonArray([.. result.World.Facts.Select(fact => (JsonNode?)JsonValue.Create(plan.Events[fact.EventContract].Name))]);

        return outcome;
    }

    internal static async Task Verify(SemanticExecutionPlan plan, JsonNode vector)
    {
        var evaluator = new SemanticEvaluator();
        var prefix = vector["decided"]!.AsArray().Select(fact => Fact(plan, fact!)).ToImmutableArray();
        var decided = Assert.IsType<SemanticAccepted>(evaluator.EstablishWorld(plan, prefix)).World;
        var decision = Decide(plan, decided, vector["request"]!);
        var dependencies = new JsonArray();
        foreach (var entry in decision.Dependencies.Entries)
        {
            dependencies.Add(new JsonObject
            {
                ["key"] = ((SemanticTextValue)entry.Key).Value,
                ["eventContracts"] = new JsonArray([.. entry.EventContracts.Select(id => plan.Events[id].Name).Order(StringComparer.Ordinal).Select(value => (JsonNode?)JsonValue.Create(value))])
            });
        }
        Assert.True(JsonNode.DeepEquals(dependencies, vector["dependencies"]), $"{vector["name"]}: dependencies differ.");

        // Each entry is an alternative durable log, not a continuation of a successful append.
        // The very same prepared decision is passed through the Host's settlement and guarded-persistence path.
        foreach (var commit in vector["commits"]!.AsArray())
        {
            var log = new a_semantic_fact_log();
            var initial = decided;
            if (commit!["history"] is JsonArray history)
            {
                initial = Assert.IsType<SemanticAccepted>(evaluator.EstablishWorld(plan, [.. history.Select(fact => Fact(plan, fact!))])).World;
                log.Facts.AddRange(initial.Facts);
            }
            else
            {
                log.Facts.AddRange(prefix);
                log.Facts.AddRange(commit["later"]!.AsArray().Select(fact => Fact(plan, fact!)));
            }

            using var runtime = log.Runtime(plan, initial);
            var result = await runtime.Commit(decision.Commit, new(DateTimeOffset.UnixEpoch, "owner", "Owner", "owner"));
            var actual = Outcome(plan, result);
            Assert.True(JsonNode.DeepEquals(actual, commit["expect"]), $"{vector["name"]}: expected {commit["expect"]}, observed {actual}");
            Assert.Null(runtime.FaultReason);
            Assert.Equal(result is SemanticAccepted ? 1 : 0, log.Writes);
        }
    }

    static ImmutableArray<SemanticPropertyValue> Values(SemanticCommand command, JsonObject values) =>
        [.. values.Select(pair => new SemanticPropertyValue(command.Properties.Single(property => property.Name == pair.Key).Id, SemanticValue.Text(pair.Value!.GetValue<string>())))];

    static JsonObject Rejected(SemanticRejected rejected)
    {
        var outcome = new JsonObject { ["outcome"] = "rejected", ["category"] = rejected.Category.ToString() };
        if (!string.IsNullOrEmpty(rejected.Code)) outcome["code"] = rejected.Code;
        outcome["message"] = rejected.Details;

        return outcome;
    }

    static JsonObject Observed(SemanticExecutionPlan plan, SemanticFact fact)
    {
        var contract = plan.Events[fact.EventContract];
        var observed = new JsonObject { ["event"] = contract.Name, ["source"] = ((SemanticTextValue)fact.Destination).Value };
        if (fact.Values.Length > 0)
        {
            var values = new JsonObject();
            foreach (var property in contract.Properties)
            {
                values[property.Name] = fact.Values.Single(value => value.TargetProperty == property.Id).Value switch
                {
                    SemanticTextValue text => text.Value,
                    SemanticNullValue => null,
                    _ => throw new InvalidSemanticContract("A race vector fact holds a value that is not text.")
                };
            }
            observed["values"] = values;
        }

        return observed;
    }
}
