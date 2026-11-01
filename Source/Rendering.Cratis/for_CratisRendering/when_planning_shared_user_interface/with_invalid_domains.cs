// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_planning_shared_user_interface;

public class with_invalid_domains : given.a_partitioned_screen_plan
{
    static readonly PlanSelection _registration = new([PlanSelectionEntry.Slice("Projects", "Registration", "RegisterProject")]);
    static readonly PlanSelection _lookup = new([PlanSelectionEntry.Slice("Projects", "Registration", "ProjectLookup")]);

    CratisPlanResult _withoutUserInterface = null!;
    CratisPlanResult _reserved = null!;
    CratisPlanResult _ownDomain = null!;
    CratisPlanResult _repeated = null!;
    CratisPlanResult _twice = null!;
    CratisPlanResult _unknown = null!;

    void Because()
    {
        var options = _options with { Domain = "Sales", UserInterface = new([]) };
        _withoutUserInterface = Plan(options with { UserInterface = null, UserInterfaceDomains = [new("Support", _lookup)] });
        _reserved = Plan(options with { UserInterfaceDomains = [new("Customizations", _lookup)] });
        _ownDomain = Plan(options with { UserInterfaceDomains = [new("sales", _lookup)] });
        _repeated = Plan(options with { UserInterfaceDomains = [new("Support", _lookup), new("support", new([]))] });
        _twice = Plan(options with { UserInterface = _lookup, UserInterfaceDomains = [new("Support", _lookup)] });
        _unknown = Plan(options with { UserInterfaceDomains = [new("Support", new([PlanSelectionEntry.Slice("Projects", "Registration", "Missing")]))] });
    }

    [Fact] void should_require_the_user_interface() => Code(_withoutUserInterface).ShouldEqual("STAGE-PLAN-024");
    [Fact] void should_refuse_a_reserved_domain() => Code(_reserved).ShouldEqual("STAGE-PLAN-025");
    [Fact] void should_refuse_the_plans_own_domain() => Code(_ownDomain).ShouldEqual("STAGE-PLAN-026");
    [Fact] void should_refuse_a_repeated_domain() => Code(_repeated).ShouldEqual("STAGE-PLAN-026");
    [Fact] void should_refuse_a_slice_present_under_two_domains() => Code(_twice).ShouldEqual("STAGE-PLAN-027");
    [Fact] void should_say_which_domain_names_an_unknown_slice() => _unknown.Diagnostics.Single().Message.StartsWith("User interface domain 'Support': ", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_plan_nothing() => new[] { _withoutUserInterface, _reserved, _ownDomain, _repeated, _twice, _unknown }.All(result => result.Artifacts.IsEmpty).ShouldBeTrue();

    static string Code(CratisPlanResult result) => result.Diagnostics.Single().Code;

    CratisPlanResult Plan(CratisPlanOptions options) => CratisRendering.PlanFrom(_loaded, _registration, options);
}
