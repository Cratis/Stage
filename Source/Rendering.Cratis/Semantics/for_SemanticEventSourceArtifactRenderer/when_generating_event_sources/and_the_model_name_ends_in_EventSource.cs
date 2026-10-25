// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateChangeArtifactRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticEventSourceArtifactRenderer.when_generating_event_sources;

public class and_the_model_name_ends_in_EventSource : a_routed_command
{
    string _definition = null!;

    void Establish() => _source = _source with { Name = "AccountEventSource" };
    void Because()
    {
        Plan();
        _definition = Text(_plan.Artifacts.Single(artifact => artifact.RelativePath == "EventSources/AccountEventSourceEventSource.cs"));
    }

    [Fact] void should_append_the_suffix_even_if_the_model_has_it() => _definition.ShouldContain("class AccountEventSourceEventSource");
    [Fact] void should_reference_the_recoverable_class() => _code.ShouldContain("AccountEventSourceEventSource>(AccountEventSourceEventSource.Transactions)");
}
