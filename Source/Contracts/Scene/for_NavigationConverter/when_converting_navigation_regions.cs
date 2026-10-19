// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Common;
using Cratis.Scene.Model.Navigation;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_NavigationConverter;

public class when_converting_navigation_regions : given.a_translated_application
{
    const string Shell =
        """
        layout AppShell
          category application
          content
          side

          navigation topbar
            logo image "/acme.svg" title "Acme"
            item sales title "Sales" icon receipt target Shop
            item support title $strings.nav.support target Support.Tickets slot side
          navigation sidebar
            item shop title "Shop" target Shop
              item orders title "Orders" target Shop.Orders
              item returns title "Returns" target Shop.Returns slot side

        module Shop
          navigation sidebar extends
            item more title "More" target Shop.Returns
          navigation topbar overrides
            item mine title "Mine" target Shop.Orders
          feature Orders
            slice StateView OrderList
              screen List
                title "Orders"
          feature Returns

        module Support
          navigation sidebar inherits
          feature Tickets

        module Empty
        """;

    NavigationModel Navigation => _application.Navigation;

    NavigationRegion Region(NavigationRegionKind kind) => Navigation.Application.Regions.Single(region => region.Region == kind);

    void Because() => Translate(Shell);

    [Fact] void should_convert_both_application_regions() => Navigation.Application.Regions.Select(region => region.Region).ShouldContainOnly(NavigationRegionKind.Topbar, NavigationRegionKind.Sidebar);
    [Fact] void should_leave_the_application_regions_without_a_mode() => Navigation.Application.Regions.All(region => region.Mode is null).ShouldBeTrue();
    [Fact] void should_convert_the_logo() => (Navigation.Application.Logo!.Image, Navigation.Application.Logo.Title).ShouldEqual(("/acme.svg", "Acme"));
    [Fact] void should_convert_the_item_title_icon_and_module_target() => (Region(NavigationRegionKind.Topbar).Items[0].Title, Region(NavigationRegionKind.Topbar).Items[0].Icon, Region(NavigationRegionKind.Topbar).Items[0].Target).ShouldEqual(("Sales", "receipt", new DestinationReference(Module: "Shop")));
    [Fact] void should_convert_a_feature_target_and_slot() => (Region(NavigationRegionKind.Topbar).Items[1].Target, Region(NavigationRegionKind.Topbar).Items[1].Slot).ShouldEqual((new DestinationReference(Module: "Support", Feature: "Tickets"), "side"));
    [Fact] void should_keep_a_localized_title_as_written() => Region(NavigationRegionKind.Topbar).Items[1].Title.ShouldEqual("$strings.nav.support");
    [Fact] void should_convert_sub_items() => Region(NavigationRegionKind.Sidebar).Items[0].Children!.Select(item => item.Id).ShouldContainOnly("orders", "returns");
    [Fact] void should_leave_an_item_without_sub_items_with_none() => Region(NavigationRegionKind.Topbar).Items[0].Children.ShouldBeNull();
    [Fact] void should_describe_only_the_modules_that_declare_regions() => Navigation.Modules.Select(module => module.Module).ShouldContainOnly("Shop", "Support");
    [Fact] void should_convert_an_extending_region() => Navigation.Modules.Single(module => module.Module == "Shop").Regions.Single(region => region.Region == NavigationRegionKind.Sidebar).Mode.ShouldEqual(NavigationRegionMode.Extend);
    [Fact] void should_convert_an_overriding_region() => Navigation.Modules.Single(module => module.Module == "Shop").Regions.Single(region => region.Region == NavigationRegionKind.Topbar).Mode.ShouldEqual(NavigationRegionMode.Override);
    [Fact] void should_convert_an_inheriting_region() => Navigation.Modules.Single(module => module.Module == "Support").Regions.Single().Mode.ShouldEqual(NavigationRegionMode.Inherit);
    [Fact] void should_convert_the_items_of_a_module_region() => Navigation.Modules.Single(module => module.Module == "Shop").Regions.First().Items.Single().Id.ShouldEqual("more");
    [Fact] void should_report_no_findings() => Cratis.Stage.Contracts.Scene.RenderFindings.ForStructure(_application).ShouldBeEmpty();
}

public class when_a_document_declares_no_navigation : given.a_translated_application
{
    void Because() => Translate(Model);

    [Fact] void should_have_no_application_regions() => _application.Navigation.Application.Regions.ShouldBeEmpty();
    [Fact] void should_have_no_logo() => _application.Navigation.Application.Logo.ShouldBeNull();
    [Fact] void should_have_no_module_navigation() => _application.Navigation.Modules.ShouldBeEmpty();
}

// Decision 0098 (#868): a button in the layout's topbar region belongs to the application, and Scene's navigation regions carry items only.
public class when_the_topbar_declares_application_toolbar_buttons : given.a_translated_application
{
    const string Shell =
        """
        policy IsBuyer
          require role "buyer"

        layout AppShell
          category application
          content

          navigation topbar
            item shop title "Shop" target Shop
            button place title "Place order" slice Shop.Orders.PlaceOrder emphasis primary icon plus
              authorize IsBuyer

        module Shop
          feature Orders
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId Uuid identifier
                produces OrderPlaced
                  orderId = orderId
              event OrderPlaced
                orderId Uuid
        """;

    void Because()
    {
        var compiled = new Cratis.Screenplay.ScreenplayCompiler().Compile(Shell);
        Assert.True(compiled.Success, string.Join("; ", compiled.Diagnostics.Select(diagnostic => $"{diagnostic.Code} {diagnostic.Message}")));
        _application = new ScreenplaySceneVisitor().Convert(compiled.Value!);
    }

    [Fact] void should_still_convert_the_items_beside_the_button() => _application.Navigation.Application.Regions.Single().Items.Single().Id.ShouldEqual("shop");
    [Fact] void should_say_that_the_button_was_omitted() => _application.ConversionDiagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-SCENE-NAV-001");
    [Fact] void should_name_the_button() => _application.ConversionDiagnostics.Single(diagnostic => diagnostic.Code == "STAGE-SCENE-NAV-001").Message.ShouldContain("'place'");
}
