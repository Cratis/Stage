// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures.when_inspecting_reactions;

public class with_caused_by_context : given.an_event_reaction
{
    readonly List<SemanticVersionFeature> _refusals = [];

    void Because()
    {
        foreach (var kind in new[] { SemanticEventContextValueKind.CausedBySubject, SemanticEventContextValueKind.CausedByName, SemanticEventContextValueKind.CausedByUserName })
        {
            var expression = new SemanticEventContextExpression(kind, new(SemanticTypeReferenceKind.Primitive, SemanticPrimitiveType.Text, default, false, false));
            var produced = _trigger.Produces.Single();
            foreach (var trigger in new[]
            {
                _trigger with { Produces = [produced with { Mappings = [new(produced.Mappings.Single().TargetProperty, expression)] }] },
                _trigger with { Produces = [produced with { Destination = expression }] },
                _trigger with { Produces = [], Invokes = [new(_slice.Id, [new(produced.Mappings.Single().TargetProperty, expression)])] }
            })
            {
                var slice = _slice with { Reactions = [_reaction with { Triggers = [trigger] }] };
                _refusals.Add(SemanticVersionFeatures.InSlice(slice).First());
            }
        }
    }

    [Fact] void should_report_occurrence_context_for_mappings_destinations_and_invocations() => _refusals.Select(feature => feature.Code).Distinct().ShouldContainOnly(["STAGE-ESM-013"]);
    [Fact] void should_report_the_occurrence_capability() => _refusals.Select(feature => feature.Capability).Distinct().ShouldContainOnly([StageExecutionCapability.Occurrence]);
    [Fact] void should_name_every_caused_by_member() => _refusals.Select(feature => feature.Message).Distinct().ShouldContainOnly([
        "Reaction 'ReminderScheduler' maps $context.causedBy.subject, which Stage does not support yet.",
        "Reaction 'ReminderScheduler' maps $context.causedBy.name, which Stage does not support yet.",
        "Reaction 'ReminderScheduler' maps $context.causedBy.userName, which Stage does not support yet."]);
    [Fact] void should_check_all_three_expression_positions() => _refusals.Count.ShouldEqual(9);
}
