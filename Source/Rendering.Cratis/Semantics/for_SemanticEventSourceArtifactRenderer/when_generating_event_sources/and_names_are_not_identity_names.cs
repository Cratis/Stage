// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateChangeArtifactRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticEventSourceArtifactRenderer.when_generating_event_sources;

public class and_names_are_not_identity_names : a_routed_command
{
    string _definition = null!;

    void Establish() => _source = _source with { Name = "account", Streams = [_source.Streams[0] with { Name = "Account_Name", StreamKind = "stored-transactions" }] };
    void Because()
    {
        Plan();
        _definition = Text(_plan.Artifacts.Single(artifact => artifact.RelativePath == "EventSources/AccountEventSource.cs"));
    }

    [Fact] void should_preserve_the_existing_definition_bytes() => _definition.ShouldEqual("""
        namespace InvoiceApp.EventSources;

        /// <summary>
        /// Defines the account event source.
        /// </summary>
        [global::Cratis.Chronicle.EventSources.EventSourceAttribute("Account")]
        [global::Cratis.Chronicle.EventSources.EventStreamAttribute("stored-transactions")]
        public class AccountEventSource : global::Cratis.Chronicle.EventSources.IEventSource;

        """);
    [Fact] void should_preserve_the_literal_command_route() => _code.Contains("AccountEventSource>(\"stored-transactions\")", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_declare_a_normalized_stream_constant() => _definition.Contains("const string", StringComparison.Ordinal).ShouldBeFalse();
}
