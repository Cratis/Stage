// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_reaction_appends;

public class without_a_reaction : for_SemanticReactionSpecificationRenderer.given.a_reaction_specification
{
    void Establish() => ReplaceAutomation(_located.Slice with { Reactions = [] });
    void Because() => Validate();
    [Fact] void should_keep_non_reaction_appends_refused() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(["STAGE-ESM-011"]);
}
