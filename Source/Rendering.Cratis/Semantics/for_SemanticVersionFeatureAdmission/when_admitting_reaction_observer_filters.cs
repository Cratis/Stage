// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatureAdmission;

public class when_admitting_reaction_observer_filters : for_SemanticReactionSpecificationRenderer.given.a_filtered_reaction
{
    void Because() => Plan();
    [Fact] void should_admit_the_filter() => _plan.Success.ShouldBeTrue();
    [Fact] void should_emit_no_refusals() => _plan.Diagnostics.ShouldBeEmpty();
}
