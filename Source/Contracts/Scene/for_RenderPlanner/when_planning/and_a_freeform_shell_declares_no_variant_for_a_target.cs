// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Layouts;
using Cratis.Scene.Model.SizeClasses;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene.for_RenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_RenderPlanner.when_planning;

public class and_a_freeform_shell_declares_no_variant_for_a_target : an_application_targeting_web_and_ios
{
    ApplicationRenderPlan _result = null!;

    void Establish() => _application = _application with
    {
        Layouts =
        [
            _application.Layouts[0] with
            {
                Arrangement = new FreeformSlotArrangement(
                [
                    new FreeformSlotVariant(
                        new SizeClass(WidthSizeClass.Regular, HeightSizeClass.Regular),
                        [new SlotPlacement("navigation", 0, 0, 240, 1024), new SlotPlacement("content", 240, 0, 1080, 1024)]),
                ]),
            },
        ],
    };

    void Because() => _result = RenderPlanner.Plan(_application, _catalog);

    [Fact] void should_place_the_shells_slots_on_the_target_the_variant_targets() => Web().Arrangements.Single().Slots!.Placements.Count.ShouldEqual(2);
    [Fact] void should_consider_that_target_renderable() => Web().IsComplete.ShouldBeTrue();
    [Fact] void should_report_the_missing_variant_on_the_target_nothing_targets() => Finding()!.Kind.ShouldEqual(RenderFindingKind.SizeClassVariantMissing);
    [Fact] void should_name_the_structure_with_no_variant() => Finding()!.Subject.ShouldEqual("AppShell");
    [Fact] void should_select_no_arrangement_for_that_target() => Ios().Arrangements.ShouldBeEmpty();

    RenderPlan Web() => _result.Targets.First(target => target.Profile.TargetPlatform == "web");

    RenderPlan Ios() => _result.Targets.First(target => target.Profile.TargetPlatform == "ios");

    RenderFinding? Finding() => Ios().Findings.FirstOrDefault(finding => finding.Kind == RenderFindingKind.SizeClassVariantMissing);
}
