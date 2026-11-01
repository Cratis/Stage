// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_planning_the_test_kit;

public class and_the_kit_omits_a_reaction : given.a_declarative_planner
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = Plan(Profile(Archive(KitFiles(Manifest.Replace("\"forEach\":\"model.items.reactions\"", "\"forEach\":\"model.items.reactions\",\"when\":\"false\"", StringComparison.Ordinal)))));

    [Fact] void should_register_no_consumer_for_the_reaction() => _plan.Inventory!.Constructs.Single(_ => _.Kind == ArtifactRenderConstructKind.Reaction).Degradations.Single().PlaceholderKind.ShouldEqual(ArtifactRenderPlaceholderKind.UnregisteredConsumer);
    [Fact] void should_render_both_commands() => _plan.Inventory!.Constructs.Count(_ => _.Kind == ArtifactRenderConstructKind.Command && _.Outcome == ArtifactRenderOutcome.Rendered).ShouldEqual(2);
}
