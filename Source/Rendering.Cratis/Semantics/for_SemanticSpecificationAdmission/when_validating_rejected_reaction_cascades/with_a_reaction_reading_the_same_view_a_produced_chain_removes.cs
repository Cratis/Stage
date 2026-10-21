// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_rejected_reaction_cascades;

public class with_a_reaction_reading_the_same_view_a_produced_chain_removes : for_SemanticReactionSpecificationRenderer.given.a_reaction_specification
{
    // Started -> Noted -> Escalated -> Archived only through reactions that produce, so no further command is invoked along the chain.
    void Establish() => Prepare(with_a_reaction_reading_the_same_view.Source
        .Replace(
            "      specification StartingAnUnknownItem",
            "      event Noted\n      event Escalated\n      reaction Noting\n        when Started\n          produces Noted\n      reaction Escalating\n        when Noted\n          produces Escalated\n      reaction Archiving\n        when Escalated\n          produces Archived\n      specification StartingAnUnknownItem",
            StringComparison.Ordinal));

    bool _rejectedByTrigger;

    void Because() => _rejectedByTrigger = SemanticReactionCascade.IsRejectedByTrigger(_context, _specification);

    [Fact] void should_not_treat_the_rejection_as_the_triggering_command_own() => _rejectedByTrigger.ShouldBeFalse();
}
