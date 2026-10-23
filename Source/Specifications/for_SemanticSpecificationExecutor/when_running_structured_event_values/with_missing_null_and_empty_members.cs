// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Specifications.Types;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_structured_event_values;

public class with_missing_null_and_empty_members : given.a_structured_event
{
    JsonElement _entries;
    JsonElement _details;
    JsonElement _numbers;

    void Establish() => Compile();
    void Because()
    {
        var fact = _plan.Specifications.Values.Single().GivenEvents.Single();
        var properties = _plan.Events[fact.EventContract].Properties;
        var detailsType = _plan.Model.Application.Types.Single();
        var tags = detailsType.Properties.Single(property => property.Name == "tags").Id;
        var label = detailsType.Properties.Single(property => property.Name == "label").Id;
        fact = fact with { Values = [.. fact.Values.Select(member => properties.Single(property => property.Id == member.TargetProperty).Name switch
        {
            "details" => member with { Value = new SemanticCompositeValue([new(label, SemanticValue.Null)]) },
            "entries" => member with { Value = new SemanticArrayValue([.. ((SemanticArrayValue)member.Value).Values.Take(2), new SemanticCompositeValue([new(tags, SemanticValue.Null)])]) },
            _ => member
        })] };
        var instance = new SemanticRuntimeTypes(_plan).Materialize(fact);
        _entries = ((SemanticStructuredEventValue)instance.GetType().GetProperty("entries")!.GetValue(instance)!).Value;
        _details = ((SemanticStructuredEventValue)instance.GetType().GetProperty("details")!.GetValue(instance)!).Value;
        _numbers = ((SemanticStructuredEventValue)instance.GetType().GetProperty("numbers")!.GetValue(instance)!).Value;
    }

    [Fact] void should_keep_an_absent_member() => _entries[0].TryGetProperty("tags", out _).ShouldBeFalse();
    [Fact] void should_keep_an_empty_collection() => _entries[1].GetProperty("tags").GetArrayLength().ShouldEqual(0);
    [Fact] void should_keep_a_null_collection() => _entries[2].GetProperty("tags").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_keep_a_null_member() => _details.GetProperty("label").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_keep_primitive_collection_elements() => _numbers[1].GetInt32().ShouldEqual(2);
}
