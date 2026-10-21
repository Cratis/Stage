// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_rejected_reaction_cascades;

public class with_a_reaction_reading_the_same_view_a_refusal_branch_removes : for_SemanticReactionSpecificationRenderer.given.a_reaction_specification
{
    // The refusal branch of the invoked command produces Abandoned, which leads to Archived through two producing reactions.
    void Establish() => Prepare(with_a_reaction_reading_the_same_view.Source
        .Replace(
            "            id = id\n      specification StartingAnUnknownItem",
            "            id = id\n            on refused\n              produces Abandoned\n      event Abandoned\n      event Escalated\n      reaction Escalating\n        when Abandoned\n          produces Escalated\n      reaction Archiving\n        when Escalated\n          produces Archived\n      specification StartingAnUnknownItem",
            StringComparison.Ordinal));

    bool _rejectedByTrigger;

    void Because() => _rejectedByTrigger = SemanticReactionCascade.IsRejectedByTrigger(_context, _specification);

    [Fact] void should_not_treat_the_rejection_as_the_triggering_command_own() => _rejectedByTrigger.ShouldBeFalse();
}
