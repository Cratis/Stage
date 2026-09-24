// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticHttpBinding.given;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHttpBinding.when_posting;

public class with_valid_payload : a_bound_semantic_model
{
    int _status;
    string _body = null!;

    async Task Because() => (_status, _body, _) = await Request("POST", Route, Payload);

    [Fact] void should_accept_the_bound_command() => _status.ShouldEqual(200);
    [Fact] void should_echo_the_payload() => JsonDocument.Parse(_body).RootElement.GetProperty("response").GetProperty("name").GetString().ShouldEqual("Screenplay");
    [Fact] async Task should_append_the_fact() => await _facts.Received(1).Append(Arg.Is<IReadOnlyList<SemanticFact>>(facts => facts.Count == 1), Arg.Any<SemanticCommandOccurrence>());
}
