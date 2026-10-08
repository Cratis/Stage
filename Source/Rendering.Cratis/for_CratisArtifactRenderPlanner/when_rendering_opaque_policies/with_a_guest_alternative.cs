// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_opaque_policies;

/// <summary>
/// An opaque body is unknown until it runs, so only a portable alternative can opt an operation into guest evaluation.
/// </summary>
public class with_a_guest_alternative : Specification
{
    const string Body = "return true;";
    ExecutableSemanticModel _model = null!;
    Assembly _assembly = null!;

    void Because()
    {
        var loaded = opaque_policy_model.Load(opaque_policy_model.Source(Body, "Guests or Custom", "Custom"));
        _model = loaded.Model;
        var plan = opaque_policy_model.Plan(loaded);
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        _assembly = opaque_policy_model.Compile(plan);
    }

    [Fact] void should_evaluate_guests_for_a_portable_guest_alternative() => opaque_policy_model.EvaluatesAnonymous(_assembly, Operation(slice => slice.Commands.Select(command => command.Id))).ShouldBeTrue();
    [Fact] void should_not_evaluate_guests_for_an_opaque_policy_alone() => opaque_policy_model.EvaluatesAnonymous(_assembly, Operation(slice => slice.Queries.Select(query => query.Id))).ShouldBeFalse();

    SemanticId Operation(Func<SemanticSlice, IEnumerable<SemanticId>> select) =>
        _model.Application.Modules.Single().Features.Single().Slices.SelectMany(select).Single();
}
#endif
