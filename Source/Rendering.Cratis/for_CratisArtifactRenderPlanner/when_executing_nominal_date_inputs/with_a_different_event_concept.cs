// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Runtime.CompilerServices;
using Cratis.Chronicle.EventSequences;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_nominal_date_inputs;

public class with_a_different_event_concept : Specification
{
    object _response;
    object _eventValue;

    void Establish()
    {
        var assembly = datetime_concept_model.Load(datetime_concept_model.Source("RecordedMoment"));
        var command = datetime_concept_model.Bind(assembly, "{\"id\":\"one\",\"from\":\"2026-10-17T00:00:00Z\",\"to\":\"2026-10-17T02:00:00.1234567+02:00\"}");
        var result = (ITuple)datetime_concept_model.Command(assembly).GetMethod("Handle").Invoke(command, null);
        _response = result[0];
        var produced = (EventForEventSourceId)result[1];
        _eventValue = produced.Event.GetType().GetProperty("At").GetValue(produced.Event);
    }

    [Fact] void should_keep_the_nominal_response() => _response.GetType().Name.ShouldEqual("Moment");
    [Fact] void should_construct_the_destination_concept() => _eventValue.GetType().Name.ShouldEqual("RecordedMoment");
    [Fact] void should_preserve_the_mapped_value() => Value(_eventValue).EqualsExact(Value(_response)).ShouldBeTrue();

    static DateTimeOffset Value(object concept) => Assert.IsType<DateTimeOffset>(concept.GetType().GetProperty("Value").GetValue(concept));
}
#endif
