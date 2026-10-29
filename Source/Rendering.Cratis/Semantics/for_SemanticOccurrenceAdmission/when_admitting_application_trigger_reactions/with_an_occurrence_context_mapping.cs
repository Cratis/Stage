// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOccurrenceAdmission.when_admitting_application_trigger_reactions;

public class with_an_occurrence_context_mapping : given.an_application_trigger_reaction
{
    readonly Dictionary<SemanticEventContextValueKind, string> _messages = [];

    void Because()
    {
        var produced = _trigger.Produces.Single();
        foreach (var member in new[] { SemanticEventContextValueKind.Occurred, SemanticEventContextValueKind.EventSourceIdentity })
        {
            _trigger = _trigger with { Produces = [produced with { Mappings = [new(produced.Mappings.Single().TargetProperty,
                new SemanticEventContextExpression(member, new(SemanticTypeReferenceKind.Primitive, SemanticPrimitiveType.Text, default, false, false)))] }] };
            Inspect();
            _messages[member] = _diagnostics.Single(item => item.Artifact == _reaction.Id && item.Code == "STAGE-ESM-024").Message;
        }
    }

    [Theory]
    [InlineData(SemanticEventContextValueKind.Occurred, "occurred")]
    [InlineData(SemanticEventContextValueKind.EventSourceIdentity, "eventSourceIdentity")]
    public void should_refuse_an_occurrence_context_mapping(SemanticEventContextValueKind member, string name)
    {
        _messages[member].ShouldContain($"$context.{name}");
        _messages[member].ShouldContain("ProcessBatch");
        _messages[member].ShouldContain("BatchRequested");
    }
}
