// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticCommandOutcome;

public class when_mapping_unauthorized : Specification
{
    CommandResult _result = null!;

    void Because() => _result = SemanticCommandOutcome.Map(
        new SemanticRejected(SemanticWorld.Empty, SemanticRejectionCategory.Unauthorized, null, "Caller is not authorized."),
        CorrelationId.NotSet,
        null,
        "command-1");

    [Fact] void should_return_unauthorized_to_arc() => _result.IsAuthorized.ShouldBeFalse();
}
