// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOccurrenceAdmission.when_admitting_application_trigger_reactions;

public class without_an_explicit_destination : given.an_application_trigger_reaction
{
    void Establish() => _trigger = _trigger with { Produces = [_trigger.Produces.Single() with { Destination = null, DestinationType = null }] };
    void Because() => Inspect();

    [Fact]
    void should_require_an_explicit_destination()
    {
        var diagnostic = _diagnostics.Single(item => item.Artifact == _reaction.Id && item.Code == "STAGE-ESM-024");
        diagnostic.Message.ShouldContain("without 'for'");
        diagnostic.Message.ShouldContain("ProcessBatch");
        diagnostic.Message.ShouldContain("BatchStarted");
    }
}
