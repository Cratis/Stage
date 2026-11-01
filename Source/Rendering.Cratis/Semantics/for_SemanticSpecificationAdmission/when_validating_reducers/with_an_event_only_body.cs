// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_reducers;

public class with_an_event_only_body : given.a_reducer_specification
{
    void Because() => Validate();
    [Fact] void should_admit_the_specification() => _diagnostics.ShouldBeEmpty();
}
