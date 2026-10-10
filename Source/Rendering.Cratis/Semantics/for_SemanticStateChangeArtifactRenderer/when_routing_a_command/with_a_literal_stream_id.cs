// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateChangeArtifactRenderer.when_routing_a_command;

public class with_a_literal_stream_id : given.a_routed_command
{
    const string Identity = "main\"\\\n|%é";

    void Establish()
    {
        _model = ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, _model.Application with { Concepts = [.. _model.Application.Concepts.Select(concept => concept.Name == "Month" ? concept with { Primitive = SemanticPrimitiveType.Text } : concept)] });
        _command = _command with { Route = _command.Route! with { StreamId = SemanticExpression.FromValue(SemanticValue.Text(Identity)) } };
    }
    void Because() => Plan();

    [Fact] void should_escape_the_formatted_literal() => _code.Contains($"EventStreamId = {CSharpCodeBuilder.StringLiteral(Identity)}", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_use_a_stream_id_attribute() => _code.Contains("EventStreamIdAttribute", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_revalidate_a_literal() => _code.Contains("TryText", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_compile() => RenderedOutput.Errors(Files()).ShouldBeEmpty();
}
