// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_reaction_appends;

public class with_a_deeper_cascade : for_SemanticReactionSpecificationRenderer.given.a_reaction_specification
{
    void Establish() => Prepare(ProductionSource + "\n      reaction Next\n        when Followed\n");
    void Because() => Validate();
    [Fact] void should_refuse_unsettled_descendants() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(["STAGE-ESM-011"]);
}
