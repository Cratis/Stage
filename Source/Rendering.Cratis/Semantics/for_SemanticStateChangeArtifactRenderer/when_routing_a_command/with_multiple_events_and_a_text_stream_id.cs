// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateChangeArtifactRenderer.when_routing_a_command;

public class with_multiple_events_and_a_text_stream_id : given.a_routed_command
{
    void Establish()
    {
        _model = ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, _model.Application with { Concepts = [.. _model.Application.Concepts.Select(concept => concept.Name == "Month" ? concept with { Primitive = SemanticPrimitiveType.Text } : concept)] });
        _command = _command with { Produces = [_command.Produces[0], _command.Produces[0]] };
    }
    void Because() => Plan();

    [Fact] void should_wrap_the_interface_return_explicitly() => _code.Contains(".FromT0(new global::Cratis.Chronicle.EventSequences.EventForEventSourceId[]", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_compile_the_oneof_interface_branch() => RenderedOutput.Errors(Files()).ShouldBeEmpty();
}
