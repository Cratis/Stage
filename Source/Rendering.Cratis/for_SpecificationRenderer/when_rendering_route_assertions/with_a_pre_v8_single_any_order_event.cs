// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer.when_rendering_route_assertions;

public class with_a_pre_v8_single_any_order_event : Specification
{
    string _code = null!;
    string _ordered = null!;
    ArtifactRenderPlan _admission = null!;

    void Because()
    {
        var model = routed_specifications.SingleAnyOrder(legacy: true);
        _admission = routed_specifications.Plan(model);
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        var options = new CratisRenderingOptions("Routed", "Routed");
        var request = new ArtifactRenderRequest(model, plan, CratisRendering.CreateProfile("Routed", options), new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, options);
        var specification = plan.Specifications.Values.Single();
        _code = SemanticCommandSpecificationRenderer.Render(specification, context).Content;
        _ordered = SemanticCommandSpecificationRenderer.Render(specification with { ThenEventsInAnyOrder = false }, context).Content;
    }

    [Fact] void should_keep_the_single_event_path_byte_for_byte() => _code.ShouldEqual(_ordered);
    [Fact] void should_keep_the_original_append_assertion() => _code.ShouldContain("ShouldHaveAppendedEvent<global::Routed.Banking.Deposits.Deposit.Deposit, global::Routed.Banking.Deposits.Deposit.Deposited>(\"acc-1\", @event => true)");
    [Fact] void should_still_refuse_conflicting_explicit_sources() => _admission.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(["STAGE-ESM-011"]);
    [Fact] void should_not_emit_an_unfaithful_specification() => _admission.Artifacts.ShouldBeEmpty();
}
