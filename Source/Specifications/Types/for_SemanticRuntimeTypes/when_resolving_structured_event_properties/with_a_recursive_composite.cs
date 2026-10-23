// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Stage.Specifications.Types.for_SemanticRuntimeTypes.when_resolving_structured_event_properties;

public class with_a_recursive_composite : given.a_structured_event_contract
{
    [Fact] void should_keep_the_raw_json_type() => _eventType.GetProperty("chain")!.PropertyType.ShouldEqual(typeof(JsonElement));
    [Fact] void should_still_wrap_the_other_structured_properties() => typeof(SemanticStructuredEventValue).IsAssignableFrom(_eventType.GetProperty("details")!.PropertyType).ShouldBeTrue();
}
