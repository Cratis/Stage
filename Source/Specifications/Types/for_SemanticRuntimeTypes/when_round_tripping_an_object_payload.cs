// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Stage.Api;
using Cratis.Stage.Specifications.Commands;

namespace Cratis.Stage.Specifications.Types.for_SemanticRuntimeTypes;

public class when_round_tripping_an_object_payload : Specification
{
    JsonElement _payload;

    async Task Because()
    {
        var type = new DynamicTypeFactory().CreateEventType("Stage.Composite.Smoke", "CompositeFact", "f26c685f-b36f-42a8-bf5a-64cb7a5b820f", new Dictionary<string, Type> { ["details"] = typeof(object) });
        var instance = Activator.CreateInstance(type)!;
        type.GetProperty("details")!.SetValue(instance, JsonSerializer.Deserialize<JsonElement>("{\"label\":\"nested\",\"child\":{\"count\":7}}"));
        using var scenario = new EventScenario(new Defaults(new SemanticClientArtifactsProvider([type])));
        var result = await scenario.EventLog.Append(new EventSourceId("one"), instance);
        Assert.True(result.IsSuccess, string.Join(';', result.Errors));
        var persisted = Assert.Single(await scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First));
        _payload = Assert.IsType<JsonElement>(type.GetProperty("details")!.GetValue(persisted.Content));
    }

    [Fact] void should_keep_named_members() => _payload.GetProperty("label").GetString().ShouldEqual("nested");
    [Fact] void should_keep_nested_objects() => _payload.GetProperty("child").GetProperty("count").GetInt32().ShouldEqual(7);
}
