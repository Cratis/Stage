// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_planning_twice;

public class and_inputs_are_permuted : given.a_body_planner
{
    const string Reordered = """
        module Messaging
          feature Echoes
            slice Automation Notify
              reaction EchoReaction
                when Startup
            slice StateChange Reply
              command Shout
                message String
                returns message
              command Echo
                message String
                returns message
        """;

    ArtifactRenderPlan _first = null!;
    ArtifactRenderPlan _second = null!;

    void Because()
    {
        var one = Requirement("one", "a");
        var two = Requirement("two", "b");
        var profile = Profile(Archive());
        _first = Planner().Plan(Request(profile) with { ImplementationRequirements = [one, two], ImplementationContents = ImmutableDictionary<string, string>.Empty.Add("one", "a").Add("two", "b") });
        _second = Planner().Plan(Request(profile, Reordered) with { ImplementationRequirements = [two, one], ImplementationContents = ImmutableDictionary<string, string>.Empty.Add("two", "b").Add("one", "a") });
    }

    [Fact] void should_render_the_same_artifacts() => _second.Artifacts.Select(_ => (_.RelativePath, _.Sha256)).ShouldEqual(_first.Artifacts.Select(_ => (_.RelativePath, _.Sha256)));
    [Fact] void should_have_the_same_digest() => _second.Digest.ShouldEqual(_first.Digest);
    [Fact] void should_have_the_same_provenance_digest() => _second.InputProvenance.Digest.ShouldEqual(_first.InputProvenance.Digest);
}
