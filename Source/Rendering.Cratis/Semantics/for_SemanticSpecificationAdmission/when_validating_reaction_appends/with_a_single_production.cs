// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_reaction_appends;

public class with_a_single_production : for_SemanticReactionSpecificationRenderer.given.a_reaction_specification
{
    void Because() => Validate();
    [Fact] void should_admit_the_single_hop() => _diagnostics.ShouldBeEmpty();
}
