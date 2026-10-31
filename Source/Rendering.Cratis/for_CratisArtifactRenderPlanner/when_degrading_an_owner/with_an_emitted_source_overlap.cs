// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_degrading_an_owner;

public class with_an_emitted_source_overlap : given.an_unsupported_event_request
{
    ArtifactRenderPlan _retry = null!;
    Exception? _error;

    void Because()
    {
        // Inject a renderer regression at the planner's completion boundary without adding a production test hook.
        object?[] arguments = [_request, null];
        _retry = (ArtifactRenderPlan)Method("PlanCore").Invoke(null, arguments)!;
        var contaminated = ArtifactRenderPlan.Create(_request, [.. _retry.Artifacts, PlannedArtifact.CreateText("leaked-owner.cs", "unsupported", [_unsupportedEvent])], []);
        var range = Method("AdmitVersion").Invoke(null, [_request, _options]);
        _error = Assert.Throws<TargetInvocationException>(() => Method("CompletePass").Invoke(null, [contaminated, range, arguments[1]])).InnerException;
    }

    [Fact] void should_start_with_a_successful_retry() => _retry.Outcome.ShouldEqual(ArtifactRenderOutcome.Degraded);
    [Fact] void should_reject_the_retry_at_the_planner_boundary() => _error.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_identify_the_omitted_owner_overlap() => _error!.Message.ShouldEqual("An artifact realizes an omitted semantic owner.");

    static MethodInfo Method(string name) => typeof(CratisArtifactRenderPlanner).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!;
}
