// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures.when_inspecting_reactions;

public class with_precise_constructs_on_a_later_reaction : given.an_event_reaction
{
    void Establish()
    {
        var importer = _slice.Reactions.Single(reaction => reaction.Name == "PaymentImporter");
        var produced = _trigger.Produces.Single();
        var context = new SemanticEventContextExpression(SemanticEventContextValueKind.CausedByName, new(SemanticTypeReferenceKind.Primitive, SemanticPrimitiveType.Text, default, false, false));
        _slice = _slice with
        {
            Reactions = [_reaction, importer with
            {
                RunsAs = new(SemanticReactionIdentityKind.System, []),
                From = new(_slice.Id, null),
                Triggers = [importer.Triggers.Single() with
                {
                    Produces = [produced with { Mappings = [new(produced.Mappings.Single().TargetProperty, context)] }]
                }]
            }]
        };
    }

    void Because() => _features = [.. SemanticVersionFeatures.InSlice(_slice)];

    [Fact] void should_prioritize_every_precise_code_before_generic_reaction_admission() => string.Join(',', _features.Take(5).Select(feature => feature.Code)).ShouldEqual("STAGE-ESM-034,STAGE-ESM-035,STAGE-ESM-037,STAGE-ESM-013,STAGE-ESM-024");
    [Fact] void should_put_identity_first() => _features[0].Code.ShouldEqual("STAGE-ESM-034");
    [Fact] void should_keep_the_identity_message() => _features[0].Message.ShouldEqual("Reaction 'PaymentImporter' declares an ESM v10 execution identity.");
    [Fact] void should_keep_the_filter_message() => _features[1].Message.ShouldEqual("Reaction 'PaymentImporter' declares an ESM v10 observer filter.");
    [Fact]
    void should_put_the_kind_before_both_generic_reaction_refusals() => _features.Skip(4).Select(feature => feature.Message).ShouldContainOnly([
        "Reaction 'PaymentImporter' uses trigger kind 'ApplicationTrigger', which Stage does not support yet.",
        "Reaction 'ReminderScheduler' is an ESM v6 automation construct; targets must explicitly admit its event-triggered behavior.",
        "Reaction 'PaymentImporter' is an ESM v6 automation construct; targets must explicitly admit its event-triggered behavior.",
        "Slice 'Reminders' is an ESM v6 Automation slice; targets must explicitly admit its reaction behavior."]);
}
