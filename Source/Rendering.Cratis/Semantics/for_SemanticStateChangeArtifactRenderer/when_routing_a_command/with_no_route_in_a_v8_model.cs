// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateChangeArtifactRenderer.when_routing_a_command;

public class with_no_route_in_a_v8_model : given.a_routed_command
{
    ArtifactRenderPlan _legacy = null!;

    void Establish()
    {
        _command = _command with { Route = null };
        ReplaceSlice();
        _model = ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, _model.Application with { Policies = [new("NotGuest", new SemanticNotPolicyCondition(new SemanticRoleCondition("Guest")))] });
        var legacy = ExecutableSemanticModel.Create(LanguageVersion.V7, SemanticVersion.V7, _model.Application with { EventSources = [] });
        _legacy = CratisRendering.Plan(legacy, SemanticExecutionPlan.Compile(legacy).Plan!, new(ArtifactRenderScopeKind.Application, legacy.Application.Id), new("InvoiceApp", "InvoiceApp"));
    }
    void Because() => Plan();

    [Fact] void should_keep_v7_command_bytes() => _plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("/Deposit.cs", StringComparison.Ordinal)).Bytes.SequenceEqual(_legacy.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("/Deposit.cs", StringComparison.Ordinal)).Bytes).ShouldBeTrue();
    [Fact] void should_not_add_a_route_attribute() => _code.Contains("EventSourceAttribute<", StringComparison.Ordinal).ShouldBeFalse();
}
