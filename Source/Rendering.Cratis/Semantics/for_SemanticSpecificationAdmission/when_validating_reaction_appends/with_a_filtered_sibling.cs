// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_reaction_appends;

public class with_a_filtered_sibling : for_SemanticReactionSpecificationRenderer.given.a_filtered_reaction
{
    void Establish() => Prepare(FilteredSource().Replace("      specification Following", "      reaction Excluded\n        from Account.Archive\n        when Started\n          produces Followed\n            for id\n            amount = amount\n      specification Following", StringComparison.Ordinal));
    void Because() => Validate();
    [Fact] void should_admit_only_the_matching_reaction() => _diagnostics.ShouldBeEmpty();
}
