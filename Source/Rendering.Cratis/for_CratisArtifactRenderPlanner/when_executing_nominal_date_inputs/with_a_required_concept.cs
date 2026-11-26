// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Arc.Commands;
using Cratis.Chronicle.EventSequences;
using Cratis.Concepts;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_nominal_date_inputs;

public class with_a_required_concept : Specification
{
    Assembly _assembly;
    Type _command;

    void Establish()
    {
        _assembly = datetime_concept_model.Load(datetime_concept_model.Source());
        _command = datetime_concept_model.Command(_assembly);
    }

    [Fact]
    void should_keep_the_constructor_property_and_backing_type_nominal()
    {
        var concept = _assembly.GetTypes().Single(type => type.Name == "Moment");
        _command.GetProperty("From").PropertyType.ShouldEqual(concept);
        _command.GetConstructors().Single().GetParameters()[1].ParameterType.ShouldEqual(concept);
        concept.BaseType.ShouldEqual(typeof(ConceptAs<DateTimeOffset>));
    }

    [Theory]
    [InlineData("2026-10-17T00:00:00.1234567Z")]
    [InlineData("2026-10-17T02:00:00.1234567+02:00")]
    void should_preserve_ticks_offset_response_and_event_value(string instant)
    {
        var command = datetime_concept_model.Bind(_assembly, $$"""{"id":"one","from":"{{instant}}","to":"{{instant}}"}""");
        var expected = DateTimeOffset.Parse(instant, System.Globalization.CultureInfo.InvariantCulture);
        var nominal = _command.GetProperty("To").GetValue(command);
        Assert.IsType<DateTimeOffset>(nominal.GetType().GetProperty("Value").GetValue(nominal)).EqualsExact(expected).ShouldBeTrue();
        var result = (ITuple)_command.GetMethod("Handle").Invoke(command, null);
        result[0].ShouldEqual(nominal);
        var produced = (EventForEventSourceId)result[1];
        produced.Event.GetType().GetProperty("At").GetValue(produced.Event).ShouldEqual(nominal);
    }

    [Theory]
    [InlineData("2026-02-01T02:00:00+02:00", true)]
    [InlineData("2026-01-31T23:59:59.9999999Z", false)]
    void should_compare_instants_at_tick_precision(string to, bool valid)
    {
        var command = datetime_concept_model.Bind(_assembly, $$"""{"id":"one","from":"2026-02-01T00:00:00Z","to":"{{to}}"}""");
        var result = datetime_concept_model.Validate(_assembly, command);
        result.IsValid.ShouldEqual(valid);
        if (!valid) result.Errors.Single().ErrorMessage.ShouldEqual("Window reversed.");
    }

    [Fact]
    void should_refuse_offsetless_json_before_constructing_a_concept()
    {
        var refusal = Assert.Throws<ValueRequiresExplicitOffset>(() => datetime_concept_model.Bind(_assembly, "{\"id\":\"one\",\"from\":\"2026-10-17T00:00:00\",\"to\":\"2026-10-17T00:00:00Z\"}"));
        refusal.Path.ShouldEqual("$.from");
        refusal.Message.ShouldEqual("The value requires an explicit offset or Z.");
    }
}
#endif
