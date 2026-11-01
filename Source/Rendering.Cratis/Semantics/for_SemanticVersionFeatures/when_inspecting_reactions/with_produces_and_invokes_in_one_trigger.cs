// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures.when_inspecting_reactions;

public class with_produces_and_invokes_in_one_trigger : given.an_event_reaction
{
    void Establish() => _trigger = _trigger with { Invokes = _slice.Reactions.Single(reaction => reaction.Name == "PaymentImporter").Triggers.Single().Invokes };
    void Because() => _features = Inspect();

    [Fact] void should_admit_the_mixed_effects() => _features.Select(feature => feature.Code).ShouldNotContain("STAGE-ESM-037");
    [Fact] void should_keep_generic_reaction_admission() => _features.Select(feature => feature.Code).ShouldContain("STAGE-ESM-024");
}
