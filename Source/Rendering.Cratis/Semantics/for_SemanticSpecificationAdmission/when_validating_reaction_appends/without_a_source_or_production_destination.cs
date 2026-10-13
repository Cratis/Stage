// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_reaction_appends;

public class without_a_source_or_production_destination : for_SemanticReactionSpecificationRenderer.given.a_reaction_specification
{
    void Establish() => ChangeReaction(reaction => reaction with { Triggers = [reaction.Triggers.Single() with { Produces = [reaction.Triggers.Single().Produces.Single() with { Destination = null, DestinationType = null }] }] });
    void Because() => Validate();
    [Fact] void should_not_invent_the_production_destination() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(["STAGE-ESM-011"]);
}
