// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_reading_capture_state;

public class with_a_mismatched_typed_identity : given.a_translation_application
{
    void Establish() => _model = Transform(SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(33).Bytes.AsSpan()), slice => slice with
    {
        Specifications = [],
        Captures = [.. slice.Captures.Select(capture => capture with
        {
            Appends = [capture.Appends.Single() with { EventSourceType = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text) }]
        })]
    });

    void Because() => _plan = Plan(_model);

    [Fact] void should_refuse_with_the_typed_capture_diagnostic() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-052");
}
