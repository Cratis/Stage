// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_rejected_reaction_cascades;

public class with_a_triggering_command_that_can_reject : for_SemanticReactionSpecificationRenderer.given.a_reaction_specification
{
    void Establish() => Prepare(with_a_triggering_command_without_rules.Source
        .Replace("        id String identifier\n        produces Started", "        id String identifier\n        validate\n          require id != \"blocked\"\n            message \"blocked\"\n        produces Started", StringComparison.Ordinal));

    void Because() => Validate();
    [Fact] void should_keep_the_specification_refused() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(["STAGE-ESM-011"]);
    [Fact] void should_explain_that_reactions_are_not_settled() => _diagnostics.Single().Message.ShouldContain("do not settle reactions");
}
