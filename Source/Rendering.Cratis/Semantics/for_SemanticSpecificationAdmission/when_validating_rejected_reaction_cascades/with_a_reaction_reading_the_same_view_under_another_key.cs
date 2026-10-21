// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_rejected_reaction_cascades;

public class with_a_reaction_reading_the_same_view_under_another_key : for_SemanticReactionSpecificationRenderer.given.a_reaction_specification
{
    void Establish() => Prepare(with_a_reaction_reading_the_same_view.Source
        .Replace("then error \"Item 'unknown' does not exist.\"", "then error \"Item 'other' does not exist.\"", StringComparison.Ordinal));

    bool _rejectedByTrigger;

    void Because() => _rejectedByTrigger = SemanticReactionCascade.IsRejectedByTrigger(_context, _specification);

    [Fact] void should_not_treat_the_rejection_as_the_triggering_command_own() => _rejectedByTrigger.ShouldBeFalse();
}
