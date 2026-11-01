// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures.when_inspecting_reactions;

public class with_produces_and_a_refusal_branch : given.an_event_reaction
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_refuse_only_branches_that_produce_events(bool produces)
    {
        var invocation = _slice.Reactions.Single(reaction => reaction.Name == "PaymentImporter").Triggers.Single().Invokes.Single();
        _trigger = _trigger with { Invokes = [invocation with { OnRefused = [new("any", null, produces ? _trigger.Produces : [])] }] };
        var mixed = Inspect().Where(feature => feature.Code == "STAGE-ESM-037").ToArray();
        mixed.Length.ShouldEqual(produces ? 1 : 0);
        if (produces) mixed.Single().Message.ShouldContain("Cratis/Arc#3561");
    }
}
