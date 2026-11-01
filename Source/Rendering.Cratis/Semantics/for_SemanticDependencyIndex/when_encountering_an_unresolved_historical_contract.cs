// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex;

public class when_encountering_an_unresolved_historical_contract : given.a_dependency_index
{
    void Establish()
    {
        var source = new SemanticContextSource("current-event", Identity("missing-historical-event"), "prior") { EventRevision = EventContractRevision.Initial };
        var member = new SemanticTypedContextMember("prior", new("runtime", null, null, "string"), false, false, source);
        _request = _request with { TypedContextDescriptors = [new("body", SemanticImplementationRole.CommandHandler, 1, _registerProject.Commands.Single().Id, [member])] };
    }

    void Because() => BuildIndex();

    [Fact] void should_not_claim_completeness() => _index.IsSemanticGraphComplete.ShouldBeFalse();
    [Fact] void should_explain_the_unresolved_historical_event() => _index.IncompleteReasons.Any(reason => reason.Contains("SemanticContextSource.SemanticId: unresolved identity", StringComparison.Ordinal)).ShouldBeTrue();
}
