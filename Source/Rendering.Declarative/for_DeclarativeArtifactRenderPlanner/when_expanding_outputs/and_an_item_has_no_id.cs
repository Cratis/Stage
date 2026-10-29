// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_expanding_outputs;

public class and_an_item_has_no_id : given.a_declarative_planner
{
    Contracts.Rendering.ArtifactRenderPlan _plan = null!;

    void Because() => _plan = Plan(Profile(Archive(KitFiles(Manifest.Replace("model.items.commands", "model.scope.scopes", StringComparison.Ordinal)))));

    [Fact] void should_have_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_report_the_item_refusal() => _plan.Diagnostics.Select(_ => _.Code).ShouldEqual([DeclarativeCodes.ItemWithoutId]);
}
