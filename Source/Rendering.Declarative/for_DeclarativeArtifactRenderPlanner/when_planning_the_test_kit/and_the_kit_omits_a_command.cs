// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_planning_the_test_kit;

public class and_the_kit_omits_a_command : given.a_declarative_planner
{
    ArtifactRenderPlan _plan = null!;
    ArtifactRenderConstruct _shout = null!;

    void Because()
    {
        _plan = Plan(Profile(Archive(KitFiles(Manifest.Replace("\"forEach\":\"model.items.commands\"", "\"forEach\":\"model.items.commands\",\"when\":\"item.name == 'Echo'\"", StringComparison.Ordinal)))));
        _shout = _plan.Inventory!.Constructs.Single(_ => _.Kind == ArtifactRenderConstructKind.Command && _.Outcome == ArtifactRenderOutcome.Degraded);
    }

    [Fact] void should_still_render_the_other_outputs() => _plan.Artifacts.Select(_ => _.RelativePath).ShouldContainOnly("Commands/Echo.test", "Reactions/EchoReaction.test", "Index.test");
    [Fact] void should_degrade_exactly_the_omitted_owner() => _plan.Inventory!.Constructs.Count(_ => _.Outcome == ArtifactRenderOutcome.Degraded).ShouldEqual(1);
    [Fact] void should_record_one_unsupported_construct_degradation_for_it() => _shout.Degradations.Select(_ => _.Evidence.Diagnostic.Code).ShouldEqual([ArtifactRenderDegradationCodes.UnsupportedConstruct]);
    [Fact] void should_deny_the_enclosing_operation() => _shout.Degradations.Single().PlaceholderKind.ShouldEqual(ArtifactRenderPlaceholderKind.DenyEnclosingOperation);
    [Fact] void should_name_the_construct_in_the_warning() => _plan.Diagnostics.Single().Message.ShouldContain("'Shout'");
    [Fact] void should_own_the_warning_by_the_construct() => _plan.Diagnostics.Single().Artifact.ShouldEqual(_shout.Identity);
    [Fact] void should_score_below_one_hundred_percent() => (_plan.Inventory!.Score.BasisPoints!.Value < 10000).ShouldBeTrue();
    [Fact] void should_be_degraded() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Degraded);
    [Fact] void should_still_succeed() => _plan.Success.ShouldBeTrue();
    [Fact] void should_keep_the_input_components_through_the_inventory() => _plan.InputProvenance.Components.Length.ShouldEqual(5);
}
