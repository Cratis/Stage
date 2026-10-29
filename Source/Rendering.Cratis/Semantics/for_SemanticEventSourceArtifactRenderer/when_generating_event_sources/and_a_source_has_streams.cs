// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateChangeArtifactRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticEventSourceArtifactRenderer.when_generating_event_sources;

public class and_a_source_has_streams : a_routed_command
{
    string _definition = null!;

    void Establish()
    {
        var stream = _source.Streams[0];
        _source = _source with { Streams = [stream, stream with { Id = SemanticId.Parse($"sem1:{new string('a', 64)}"), Name = "Statements", StreamKind = "Statements", StreamIdType = null }, stream with { Id = SemanticId.Parse($"sem1:{new string('b', 64)}"), Name = "Notes", StreamKind = "Notes", StreamIdType = null }] };
    }
    void Because()
    {
        Plan();
        _definition = Text(_plan.Artifacts.Single(artifact => artifact.RelativePath == "EventSources/AccountEventSource.cs"));
    }

    [Fact] void should_use_explicit_stored_names_in_ordinal_order() => _definition.ShouldEqual("""
        using Cratis.Chronicle.EventSources;

        namespace InvoiceApp.EventSources;

        /// <summary>
        /// Defines the account event source.
        /// </summary>
        [EventSource("Account")]
        [EventStream(AccountEventSource.Notes)]
        [EventStream(AccountEventSource.Statements)]
        [EventStream(AccountEventSource.Transactions)]
        public class AccountEventSource : IEventSource
        {
            /// <summary>
            /// Identifies the notes stream.
            /// </summary>
            public const string Notes = "Notes";

            /// <summary>
            /// Identifies the statements stream.
            /// </summary>
            public const string Statements = "Statements";

            /// <summary>
            /// Identifies the transactions stream.
            /// </summary>
            public const string Transactions = "Transactions";
        }

        """);
    [Fact] void should_not_invent_concurrency() => _definition.Contains("Concurrency", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_invent_descriptions() => _definition.Contains("Description", StringComparison.Ordinal).ShouldBeFalse();
}
