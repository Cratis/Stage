// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_reaction_appends;

public class with_multiple_invocations : for_SemanticReactionSpecificationRenderer.given.a_reaction_specification
{
    void Establish()
    {
        Prepare(InvocationSource);
        ChangeReaction(reaction => reaction with { Triggers = [reaction.Triggers.Single() with { Invokes = [reaction.Triggers.Single().Invokes.Single(), reaction.Triggers.Single().Invokes.Single()] }] });
    }
    void Because() => Validate();
    [Fact] void should_refuse_more_than_one_command() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(["STAGE-ESM-011"]);
}
