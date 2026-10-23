// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Specifications.Types;

namespace Cratis.Stage.Specifications.Types.for_SemanticRuntimeTypes.when_resolving_structured_event_properties;

public class with_a_composite_property : given.a_structured_event_contract
{
    Type _shape = null!;

    void Because() => _shape = Shape("details");

    [Fact] void should_carry_the_value_in_a_structured_wrapper() => typeof(SemanticStructuredEventValue).IsAssignableFrom(_eventType.GetProperty("details")!.PropertyType).ShouldBeTrue();
    [Fact] void should_read_numbers_as_nullable_decimals() => (_shape.GetProperty("amount")!.PropertyType, _shape.GetProperty("count")!.PropertyType).ShouldEqual((typeof(decimal?), typeof(decimal?)));
    [Fact] void should_read_a_boolean_as_a_nullable_boolean() => _shape.GetProperty("flag")!.PropertyType.ShouldEqual(typeof(bool?));
    [Fact] void should_keep_text_like_members_as_text() => new[] { "label", "reference", "day", "moment" }.Select(name => _shape.GetProperty(name)!.PropertyType).All(type => type == typeof(string)).ShouldBeTrue();
}
