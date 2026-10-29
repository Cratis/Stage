// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_reducers;

public class without_a_descriptor : given.a_reducer_specification
{
    void Establish() => _request = _request with { TypedContextDescriptors = [] };
    void Because() => Validate();
    [Fact] void should_name_the_missing_descriptor() => Refusal().ShouldContain("typed context descriptor is missing");
}
