// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticHttpBinding.given;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHttpBinding.when_posting;

public class with_invalid_payload : a_bound_semantic_model
{
    int _status;
    string _body = null!;

    async Task Because() => (_status, _body, _) = await Request("POST", Route, "{\"projectId\":\"3fa85f64-5717-4562-b3fc-2c963f66afa6\",\"name\":\"\"}");

    [Fact] void should_reject_the_value() => _status.ShouldEqual(400);
    [Fact] void should_report_the_modeled_message() => _body.ShouldContain("Project name is required");
    [Fact] async Task should_not_append() => await _facts.DidNotReceive().Append(Arg.Any<IReadOnlyList<Cratis.Screenplay.Semantics.Execution.SemanticFact>>(), Arg.Any<Cratis.Screenplay.Semantics.Execution.SemanticCommandOccurrence>(), Arg.Any<ulong>());
}
