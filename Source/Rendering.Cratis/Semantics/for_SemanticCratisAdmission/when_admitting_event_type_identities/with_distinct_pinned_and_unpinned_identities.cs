// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_admitting_event_type_identities;

public class with_distinct_pinned_and_unpinned_identities : given.a_mixed_event_model
{
    void Because() => _plan = Plan("Original");

    [Fact] void should_admit_the_plan() => _plan.Success.ShouldBeTrue();
    [Fact] void should_emit_the_pinned_identity() => EventCode().ShouldContain("[global::Cratis.Chronicle.Events.EventTypeAttribute(\"Original\")]\npublic record Renamed(");
    [Fact] void should_keep_the_unpinned_identity_argument_free() => EventCode().ShouldContain("[global::Cratis.Chronicle.Events.EventTypeAttribute]\npublic record RecordedEvent(");

    string EventCode() => string.Join('\n', _plan.Artifacts.Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
}
