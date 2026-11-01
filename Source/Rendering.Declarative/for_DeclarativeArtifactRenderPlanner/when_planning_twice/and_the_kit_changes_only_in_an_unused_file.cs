// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_planning_twice;

public class and_the_kit_changes_only_in_an_unused_file : given.a_declarative_planner
{
    ArtifactRenderPlan _first = null!;
    ArtifactRenderPlan _second = null!;

    void Because()
    {
        _first = Plan(Profile(Archive()));
        var files = KitFiles();
        files["templates/header.sbn"] = Bytes("a header no output uses");
        _second = Plan(Profile(Archive(files)));
    }

    [Fact] void should_have_the_same_output_digest() => _second.Digest.ShouldEqual(_first.Digest);
    [Fact] void should_have_a_different_provenance_digest() => _second.InputProvenance.Digest.ShouldNotEqual(_first.InputProvenance.Digest);
}
