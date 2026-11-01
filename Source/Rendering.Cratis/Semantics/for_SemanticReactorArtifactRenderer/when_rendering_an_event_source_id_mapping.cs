// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer;

public class when_rendering_an_event_source_id_mapping : for_SemanticAutomationArtifactRenderer.given.an_automation
{
    const string UuidSource = """
        concept TalkId : Uuid
        module Work
          feature Processing
            slice StateChange Start
              command StartWork
                talkId TalkId identifier
                produces event Started
                  for talkId
                  talkId TalkId = talkId
            slice Automation FollowUp
              command CompleteWork
                talkId TalkId identifier
                produces event Completed
                  for talkId
                  talkId TalkId = talkId
              reaction Complete
                when Started
                  invokes CompleteWork
                    talkId = $eventSourceId
        """;

    string _text = string.Empty;
    string _uuid = string.Empty;

    void Because()
    {
        var reaction = _located.Slice.Reactions.Single(reaction => reaction.Name == "CompletePositive");
        var trigger = reaction.Triggers.Single();
        var id = _context.Events[trigger.Source].Properties.Single(property => property.Name == "id");
        var invocation = trigger.Invokes.Single();
        var mapped = invocation with
        {
            Mappings = [.. invocation.Mappings.Select(mapping => mapping.TargetProperty == invocation.Mappings[0].TargetProperty
                ? mapping with { Source = new SemanticEventContextExpression(SemanticEventContextValueKind.EventSourceIdentity, id.Type) }
                : mapping)]
        };
        _text = SemanticReactorArtifactRenderer.Render(_located, reaction with { Triggers = [trigger with { Invokes = [mapped] }] }, _context).Content;

        SetContext(Compile(UuidSource));
        _uuid = SemanticReactorArtifactRenderer.Render(_located, _located.Slice.Reactions.Single(), _context).Content;
    }

    [Fact] void should_read_a_text_identity_from_the_event_context() => _text.ShouldContain("context.EventSourceId.Value");
    [Fact] void should_parse_a_uuid_identity_into_its_concept() => _uuid.ShouldContain("new CompleteWork(new TalkId(Guid.Parse(context.EventSourceId.Value)))");
}
