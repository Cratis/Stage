// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.Commands.for_SemanticReactionBatchClaims.when_validating;

public class the_same_owner_repeating : given.a_constrained_plan
{
    void Because() => Validate([], Changed("a", "same"), Changed("a", "same"));

    [Fact] void should_accept_every_candidate() => _results.ShouldEachConformTo(result => result is null);
}
