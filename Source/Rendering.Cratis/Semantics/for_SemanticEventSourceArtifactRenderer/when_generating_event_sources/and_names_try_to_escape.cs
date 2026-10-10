// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateChangeArtifactRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticEventSourceArtifactRenderer.when_generating_event_sources;

public class and_names_try_to_escape : a_routed_command
{
    const string Stored = "\"\\\n] public class Escaped; //";
    string _definition = null!;

    void Establish() => _source = _source with { SourceKind = Stored, Streams = [_source.Streams[0] with { StreamKind = Stored }] };
    void Because()
    {
        Plan();
        _definition = Text(_plan.Artifacts.Single(artifact => artifact.RelativePath == "EventSources/AccountEventSource.cs"));
    }

    [Fact] void should_escape_stored_source_names() => _definition.Contains($"EventSourceAttribute({CSharpCodeBuilder.StringLiteral(Stored)})", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_escape_stored_stream_names() => _definition.Contains($"EventStreamAttribute({CSharpCodeBuilder.StringLiteral(Stored)})", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_compile_without_an_injected_declaration() => RenderedOutput.Errors(Files()).ShouldBeEmpty();
    [Fact] void should_not_compile_an_injected_type() => Load().GetType("InvoiceApp.EventSources.Escaped").ShouldBeNull();
}
