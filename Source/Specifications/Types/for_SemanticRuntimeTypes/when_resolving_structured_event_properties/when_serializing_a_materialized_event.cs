// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Testing;
using Cratis.Screenplay.Semantics;
using StructuredEvent = Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_structured_event_values.given.a_structured_event;

namespace Cratis.Stage.Specifications.Types.for_SemanticRuntimeTypes.when_resolving_structured_event_properties;

public class when_serializing_a_materialized_event : StructuredEvent
{
    JsonObject _json = null!;

    void Establish() => Compile();

    async Task Because()
    {
        var fact = _plan.Specifications.Values.Single().GivenEvents.Single();
        var properties = _plan.Events[fact.EventContract].Properties;
        var detailsType = _plan.Model.Application.Types.Single();
        var label = detailsType.Properties.Single(property => property.Name == "label").Id;
        fact = fact with { Values = [.. fact.Values.Select(member => properties.Single(property => property.Id == member.TargetProperty).Name == "details"
            ? member with { Value = new SemanticCompositeValue([new(label, SemanticValue.Null)]) }
            : member)] };
        var types = new SemanticRuntimeTypes(_plan);
        var instance = types.Materialize(fact);
        _json = await new Defaults(new SemanticClientArtifactsProvider([types.For(fact.EventContract)])).EventSerializer.Serialize(instance);
    }

    [Fact] void should_keep_a_null_member_as_null() => _json["details"]!["label"].ShouldBeNull();
    [Fact] void should_keep_an_absent_member_absent() => _json["details"]!.AsObject().ContainsKey("tags").ShouldBeFalse();
    [Fact] void should_keep_an_empty_collection_empty() => _json["entries"]![1]!["tags"]!.AsArray().ShouldBeEmpty();
}
