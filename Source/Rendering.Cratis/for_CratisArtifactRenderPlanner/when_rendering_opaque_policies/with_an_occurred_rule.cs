// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_opaque_policies;

public class with_an_occurred_rule : Specification
{
    const string Body = "return context.Identity.IsAuthenticated && context.Occurred.Year == 2027;";
    ArtifactRenderPlan _plan = null!;
    ExecutableSemanticModel _model = null!;
    Assembly _assembly = null!;

    void Because()
    {
        var loaded = opaque_policy_model.Load(opaque_policy_model.Source(Body));
        _model = loaded.Model;
        _plan = opaque_policy_model.Plan(loaded);
        Assert.True(_plan.Success, opaque_policy_model.Errors(_plan));
        _assembly = opaque_policy_model.Compile(_plan);
    }

    [Fact] void should_render_the_typed_policy_context() => _plan.Artifacts.Select(artifact => artifact.RelativePath).ShouldContain("TypedContexts/PolicyContext.cs");
    [Fact] void should_render_the_verified_body() => _plan.Artifacts.Select(artifact => artifact.RelativePath).ShouldContain("GeneratedPolicies/PolicyBodies.cs");
    [Fact] void should_allow_a_command_received_in_the_ruled_year() => opaque_policy_model.AllowsCommand(_assembly, _model, opaque_policy_model.Caller(), opaque_policy_model.Receipt).ShouldBeTrue();
    [Fact] void should_deny_a_command_received_in_another_year() => opaque_policy_model.AllowsCommand(_assembly, _model, opaque_policy_model.Caller(), opaque_policy_model.Receipt.AddYears(-1)).ShouldBeFalse();
    [Fact] void should_deny_a_command_without_a_receipt_time() => opaque_policy_model.AllowsCommand(_assembly, _model, opaque_policy_model.Caller(), default).ShouldBeFalse();
    [Fact] void should_deny_a_guest_command() => opaque_policy_model.AllowsCommand(_assembly, _model, opaque_policy_model.Guest(), opaque_policy_model.Receipt).ShouldBeFalse();
    [Fact] void should_allow_a_query_received_in_the_ruled_year() => opaque_policy_model.AllowsQuery(_assembly, _model, opaque_policy_model.Caller(), opaque_policy_model.Receipt, "invoice-one").ShouldBeTrue();
    [Fact] void should_deny_a_query_received_in_another_year() => opaque_policy_model.AllowsQuery(_assembly, _model, opaque_policy_model.Caller(), opaque_policy_model.Receipt.AddYears(1), "invoice-one").ShouldBeFalse();
    [Fact] void should_deny_a_query_without_a_receipt_time() => opaque_policy_model.AllowsQuery(_assembly, _model, opaque_policy_model.Caller(), default, "invoice-one").ShouldBeFalse();
    [Fact] void should_not_evaluate_guests() => opaque_policy_model.EvaluatesAnonymous(_assembly, _model.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Commands).Single().Id).ShouldBeFalse();
}
#endif
