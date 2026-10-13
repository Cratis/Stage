// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_an_omitted_optional_value : a_stored_partial_event
{
    SemanticValue? _value;

    void Establish() => Prepare("String?");

    void Because()
    {
        var fact = SemanticWorldRebuilder.Fact(_partialPlan, _stored);
        var property = _partialPlan.Events.Values.Single().Properties.Single(property => property.Name == "value");
        _value = fact.Values.Single(value => value.TargetProperty == property.Id).Value;
    }

    [Fact] void should_rebuild_the_absent_value_as_null() => _value.ShouldEqual(SemanticValue.Null);
}
