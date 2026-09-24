// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Semantics;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticCommandOutcome;

public class when_mapping_unsupported : Specification
{
    DefaultHttpContext _http = null!;
    CommandResult _result = null!;

    void Establish() => _http = new();

    void Because() => _result = SemanticCommandOutcome.Map(
        new SemanticUnsupported(SemanticWorld.Empty, SemanticExecutionCapability.IdentityAllocation, "Allocate a destination."),
        CorrelationId.NotSet,
        _http,
        "command-1");

    [Fact] void should_mark_the_http_response_for_501() => _http.Items.ContainsKey(SemanticRuntimeMarkers.Unsupported).ShouldBeTrue();
    [Fact] void should_name_the_missing_capability() => _http.Response.Headers["Stage-Unsupported-Capability"].ToString().ShouldEqual("IdentityAllocation");
    [Fact] void should_name_the_artifact() => _http.Response.Headers["Stage-Unsupported-Artifact"].ToString().ShouldEqual("command-1");
    [Fact] void should_return_an_arc_error() => _result.ExceptionMessages.Single().ShouldEqual("Unsupported(IdentityAllocation) command-1: Allocate a destination.");
}
