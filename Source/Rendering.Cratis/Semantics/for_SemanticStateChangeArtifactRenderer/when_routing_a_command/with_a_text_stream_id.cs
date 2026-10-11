// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateChangeArtifactRenderer.when_routing_a_command;

public class with_a_text_stream_id : given.a_routed_command
{
    void Establish() => _model = ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, _model.Application with { Concepts = [.. _model.Application.Concepts.Select(concept => concept.Name == "Month" ? concept with { Primitive = SemanticPrimitiveType.Text } : concept)] });
    void Because() => Plan();

    [Fact] void should_validate_text_inside_the_handler() => _code.Contains("TryText(Month.Value, out var streamIdPart0)", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_return_validation_explicitly() => _code.Contains(".FromT1(global::Cratis.Arc.Validation.ValidationResult.Error", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_return_the_event_explicitly() => _code.Contains(".FromT0(new global::Cratis.Chronicle.EventSequences.EventForEventSourceId", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_name_the_property_without_exposing_its_value() => _code.Contains("[\"Month\"]", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_compile() => RenderedOutput.Errors(Files()).ShouldBeEmpty();
}
