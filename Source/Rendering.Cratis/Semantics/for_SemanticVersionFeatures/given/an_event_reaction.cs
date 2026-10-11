// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures.given;

public class an_event_reaction : Specification
{
    protected SemanticSlice _slice = null!;
    protected SemanticReaction _reaction = null!;
    protected SemanticReactionTrigger _trigger = null!;
    private protected SemanticVersionFeature[] _features = [];

    void Establish()
    {
        var model = SemanticModelSerializer.Deserialize(ReactionsCorpus.V6.EsmBytes.AsSpan());
        _slice = SemanticVersionFeatures.Slices(model.Application).Single(slice => slice.Kind == SemanticSliceKind.Automation);
        _reaction = _slice.Reactions.Single(reaction => reaction.Name == "ReminderScheduler");
        _trigger = _reaction.Triggers.Single();
    }

    private protected SemanticVersionFeature[] Inspect() => [.. SemanticVersionFeatures.InSlice(_slice with
    {
        Reactions = [_reaction with { Triggers = [_trigger] }]
    })];
}
