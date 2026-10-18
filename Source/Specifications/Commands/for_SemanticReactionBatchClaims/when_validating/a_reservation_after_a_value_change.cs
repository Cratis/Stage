// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.Commands.for_SemanticReactionBatchClaims.when_validating;

public class a_reservation_after_a_value_change : given.a_constrained_plan
{
    void Because() => Validate([], Changed("a", "first"), Changed("a", "second"), Changed("b", "first"));

    [Fact] void should_accept_the_owner_changing_its_value() => _results[..2].ShouldEachConformTo(result => result is null);
    [Fact] void should_keep_the_reservation_the_owner_gave_up() => _results[2]!.Name.ShouldEqual("UniqueValue");
}
