// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer.when_rendering_route_assertions;

public class with_pre_v8_any_order : Specification
{
    string _code = null!;

    void Because()
    {
        var model = routed_specifications.Assignment(legacy: true);
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        var options = new CratisRenderingOptions("Routed", "Routed");
        var request = new ArtifactRenderRequest(model, plan, CratisRendering.CreateProfile("Routed", options), new(ArtifactRenderScopeKind.Application, model.Application.Id));
        _code = SemanticCommandSpecificationRenderer.Render(plan.Specifications.Values.Single(), new(request, options)).Content;
    }

    [Fact] void should_preserve_the_greedy_method_bytes() => _code.ShouldContain("""
            [global::Xunit.FactAttribute] void should_append_the_expected_event_multiset()
            {
                var remaining = _scenario.AppendedEvents.Where(entry => entry.Result.IsSuccess).ToList();
                remaining.Count.ShouldEqual(2);
                var match0 = remaining.FindIndex(entry => entry.Event.Content is global::Routed.Banking.Deposits.Deposit.Deposited @event && entry.Event.Context.EventSourceId == "acc-1");
                (match0 >= 0).ShouldBeTrue();
                remaining.RemoveAt(match0);
                var match1 = remaining.FindIndex(entry => entry.Event.Content is global::Routed.Banking.Deposits.Deposit.Deposited @event && entry.Event.Context.EventSourceId == "acc-1");
                (match1 >= 0).ShouldBeTrue();
                remaining.RemoveAt(match1);
            }
        """);
}
