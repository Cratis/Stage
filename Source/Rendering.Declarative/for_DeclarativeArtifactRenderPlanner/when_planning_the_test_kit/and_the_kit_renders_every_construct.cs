// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_planning_the_test_kit;

public class and_the_kit_renders_every_construct : given.a_declarative_planner
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = Plan(Profile(Archive()));

    [Fact] void should_render_the_command_echo() => Text(_plan, "Commands/Echo.test").ShouldEqual("command Echo");
    [Fact] void should_render_the_command_shout() => Text(_plan, "Commands/Shout.test").ShouldEqual("command Shout");
    [Fact] void should_render_the_reaction() => Text(_plan, "Reactions/EchoReaction.test").ShouldEqual("reaction EchoReaction");
    [Fact] void should_render_the_index() => Text(_plan, "Index.test").ShouldEqual("index");
    [Fact] void should_render_nothing_else() => _plan.Artifacts.Length.ShouldEqual(4);
    [Fact] void should_not_write_fully_qualified_type_names() => _plan.Artifacts.Count(_ => System.Text.Encoding.UTF8.GetString(_.Bytes.AsSpan()).Contains("global::", StringComparison.Ordinal)).ShouldEqual(0);
    [Fact] void should_be_rendered() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Rendered);
    [Fact] void should_account_for_every_command_and_reaction() => _plan.Inventory!.Constructs.Count(_ => _.Outcome == ArtifactRenderOutcome.Rendered).ShouldEqual(3);
    [Fact] void should_score_everything() => _plan.Inventory!.Score.BasisPoints.ShouldEqual(10000);
    [Fact] void should_keep_the_input_components() => _plan.InputProvenance.Components.Length.ShouldEqual(5);
}
