// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_reaction_appends;

public class with_an_unrouted_append_to_a_filtered_reaction : for_SemanticReactionSpecificationRenderer.given.a_filtered_reaction
{
    void Establish() => Prepare(FilteredSource(stream: null));
    void Because() => Validate();
    [Fact] void should_refuse_an_append_without_a_matching_reaction() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-011");
}
