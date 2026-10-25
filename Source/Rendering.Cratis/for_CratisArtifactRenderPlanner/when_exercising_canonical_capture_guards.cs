// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Serialization;
using Xunit;

#if DEBUG

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Runs the canonical capture vector's own specifications against the generated inbox reactor:
/// first arrival, a transition decided from given foreign state, and a record overlay across source events.
/// </summary>
public class when_exercising_canonical_capture_guards : given.a_translation_specification_application
{
    void Because()
    {
        _model = SemanticModelSerializer.Deserialize(PublicEventsCorpus.CapturesFromEventsV9.EsmBytes.ToArray());
        Render(clientOnly: true);
    }

    [Fact] Task should_append_added_for_the_first_record_of_a_key() => RunGenerated("when_first_arrival");
    [Fact] Task should_append_a_transition_decided_from_given_foreign_state() => RunGenerated("when_given_transition");
    [Fact] Task should_map_fields_from_the_overlaid_record() => RunGenerated("when_overlay");
}
#endif
