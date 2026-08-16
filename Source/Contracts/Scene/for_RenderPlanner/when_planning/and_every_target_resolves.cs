// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Layouts;
using Cratis.Scene.Model.SizeClasses;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene.for_RenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_RenderPlanner.when_planning;

public class and_every_target_resolves : an_application_targeting_web_and_ios
{
    ApplicationRenderPlan _result = null!;

    void Because() => _result = RenderPlanner.Plan(_application, _catalog);

    [Fact] void should_plan_one_target_per_platform() => _result.Targets.Count.ShouldEqual(2);
    [Fact] void should_report_nothing() => _result.IsComplete.ShouldBeTrue();
    [Fact] void should_expand_the_declared_packages_to_their_closure() => Web().Profile.Packages.SequenceEqual(["Tailwind", "PrimeReact"]).ShouldBeTrue();
    [Fact] void should_report_the_package_it_added_on_the_targets_behalf() => Web().Packages.Added.ShouldContainOnly("Tailwind");
    [Fact] void should_resolve_the_selected_shell() => Web().Layout!.Name.ShouldEqual("AppShell");
    [Fact] void should_resolve_the_selected_theme() => Web().Theme!.Name.ShouldEqual("Aurora");
    [Fact] void should_scope_the_themes_tokens_to_the_active_packages() => Web().ThemePackages.SequenceEqual(["Tailwind", "PrimeReact"]).ShouldBeTrue();
    [Fact] void should_resolve_a_component_only_core_declares() => Resolution("core:title").Package.ShouldEqual("core");
    [Fact] void should_resolve_a_component_to_the_highest_priority_package_declaring_it() => Resolution("core:table").Package.ShouldEqual("PrimeReact");
    [Fact] void should_record_what_the_winning_package_shadowed() => Resolution("core:table").Shadows.ShouldContainOnly("core");
    [Fact] void should_place_the_screen_template_in_the_slot_it_fits() => Web().ScreenTemplates.Placements.Single().Container.ShouldEqual("AppShell");
    [Fact] void should_assume_the_regular_size_class_for_a_target_that_declares_none() => Web().SizeClass.ShouldEqual(new SizeClass(WidthSizeClass.Regular, HeightSizeClass.Regular));
    [Fact] void should_use_the_targets_own_declared_size_class() => Ios().SizeClass.ShouldEqual(new SizeClass(WidthSizeClass.Compact, HeightSizeClass.Regular));
    [Fact] void should_lay_the_shell_out_as_a_row_on_the_wide_target() => Web().Arrangements.Single().Flow.ShouldBeOfExactType<FlowRow>();
    [Fact] void should_lay_the_same_shell_out_as_a_column_on_the_narrow_target() => Ios().Arrangements.Single().Flow.ShouldBeOfExactType<FlowColumn>();

    RenderPlan Web() => _result.Targets.First(target => target.Profile.TargetPlatform == "web");

    RenderPlan Ios() => _result.Targets.First(target => target.Profile.TargetPlatform == "ios");

    Cratis.Scene.Engine.Profiles.ComponentResolution Resolution(string name) => Web().Components.First(component => component.Name == name);
}
