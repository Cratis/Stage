// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Specifications.Types;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_structured_event_values;

public class with_missing_null_and_empty_members : given.a_structured_event
{
    Array _entries = null!;
    object _details = null!;
    Array _numbers = null!;

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
        _entries = (Array)instance.GetType().GetProperty("entries")!.GetValue(instance)!;
        _details = instance.GetType().GetProperty("details")!.GetValue(instance)!;
        _numbers = (Array)instance.GetType().GetProperty("numbers")!.GetValue(instance)!;
    }

    // Typed members cannot tell an absent member from a null one; the run refuses specifications that author an explicit null (see the projection admission).
    [Fact] void should_leave_an_absent_member_unset() => Member(_entries.GetValue(0)!, "tags").ShouldBeNull();
    [Fact] void should_keep_an_empty_collection() => ((Array)Member(_entries.GetValue(1)!, "tags")!).Length.ShouldEqual(0);
    [Fact] void should_leave_a_null_collection_unset() => Member(_entries.GetValue(2)!, "tags").ShouldBeNull();
    [Fact] void should_leave_a_null_member_unset() => Member(_details, "label").ShouldBeNull();
    [Fact] void should_keep_primitive_collection_elements() => Convert.ToInt32(_numbers.GetValue(1)).ShouldEqual(2);

    static object? Member(object value, string name) => value.GetType().GetProperty(name)!.GetValue(value);
}
