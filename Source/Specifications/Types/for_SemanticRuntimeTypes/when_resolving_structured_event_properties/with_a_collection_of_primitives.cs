// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.Types.for_SemanticRuntimeTypes.when_resolving_structured_event_properties;

public class with_a_collection_of_primitives : given.a_structured_event_contract
{
    [Fact] void should_read_text_as_a_text_array() => Shape("tags").ShouldEqual(typeof(string[]));
    [Fact] void should_read_decimals_as_a_nullable_decimal_array() => Shape("amounts").ShouldEqual(typeof(decimal?[]));
    [Fact] void should_read_a_collection_of_composites_as_an_array_of_shapes() => Shape("lines").GetElementType()!.GetProperty("amount")!.PropertyType.ShouldEqual(typeof(decimal?));
}
