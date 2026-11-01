// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOccurrenceAdmission.when_admitting_application_trigger_reactions;

public class with_other_trigger_kinds : given.an_application_trigger_reaction
{
    readonly Dictionary<SemanticReactionTriggerKind, (string Message, bool Admitted)> _results = [];

    void Because()
    {
        foreach (var kind in new[] { SemanticReactionTriggerKind.Startup, SemanticReactionTriggerKind.Shutdown,
            SemanticReactionTriggerKind.Interval, SemanticReactionTriggerKind.Schedule, SemanticReactionTriggerKind.Unknown })
        {
            _trigger = _trigger with { Kind = kind };
            Inspect();
            var diagnostic = _diagnostics.Single(item => item.Artifact == _reaction.Id && item.Code == "STAGE-ESM-024");
            _results[kind] = (diagnostic.Message, SemanticOccurrenceAdmission.IsAdmitted(_reaction with { Triggers = [_trigger] }, _context));
        }
    }

    [Theory]
    [InlineData(SemanticReactionTriggerKind.Startup, "activation identity and namespace policy")]
    [InlineData(SemanticReactionTriggerKind.Shutdown, "activation identity and namespace policy")]
    [InlineData(SemanticReactionTriggerKind.Interval, "schedules and due-time parity")]
    [InlineData(SemanticReactionTriggerKind.Schedule, "schedules and due-time parity")]
    [InlineData(SemanticReactionTriggerKind.Unknown, "unknown trigger kind")]
    public void should_name_the_remaining_target_boundary(SemanticReactionTriggerKind kind, string reason)
    {
        var result = _results[kind];
        result.Message.ShouldContain(_reaction.Name);
        result.Message.ShouldContain(reason);
        result.Message.ShouldContain("Cratis/Stage#343");
        result.Admitted.ShouldBeFalse();
    }
}
