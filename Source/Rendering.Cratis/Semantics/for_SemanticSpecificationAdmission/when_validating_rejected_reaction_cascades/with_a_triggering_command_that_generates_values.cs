// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_rejected_reaction_cascades;

public class with_a_triggering_command_that_generates_values : for_SemanticReactionSpecificationRenderer.given.a_reaction_specification
{
    void Establish() => Prepare("concept Ticket : Uuid\n  generate uuid\n" + with_a_triggering_command_without_rules.Source
        .Replace("        id String identifier\n        produces Started", "        id String identifier\n        ticket Ticket generated\n        produces Started", StringComparison.Ordinal));

    void Because() => Validate();
    [Fact] void should_keep_the_specification_refused() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-011");
}
