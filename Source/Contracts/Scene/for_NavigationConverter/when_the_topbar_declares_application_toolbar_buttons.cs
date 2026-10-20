// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_NavigationConverter;

// Decision 0098 (#868, Scene#215): a button in the layout's topbar region belongs to the application, and Scene's navigation regions carry it beside the items.
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
            button help title "Help"

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

    Cratis.Scene.Model.Navigation.NavigationRegion Topbar => _application.Navigation.Application.Regions.Single();
    Cratis.Scene.Model.Navigation.NavigationButton Place => Topbar.Buttons!.Single(button => button.Id == "place");

    [Fact] void should_still_convert_the_items_beside_the_buttons() => Topbar.Items.Single().Id.ShouldEqual("shop");
    [Fact] void should_carry_every_button() => Topbar.Buttons!.Select(button => button.Id).ShouldContainOnly("place", "help");
    [Fact] void should_carry_the_title() => Place.Title.ShouldEqual("Place order");
    [Fact] void should_write_the_slice_as_the_qualified_id() => Place.Slice.ShouldEqual("Shop.Orders.PlaceOrder");
    [Fact] void should_carry_the_emphasis() => Place.Emphasis.ShouldEqual(Cratis.Scene.Model.Toolbar.ToolbarEmphasis.Primary);
    [Fact] void should_carry_the_icon() => Place.Icon.ShouldEqual("plus");
    [Fact] void should_carry_the_policy() => Place.Authorize.ShouldEqual("IsBuyer");
    [Fact] void should_leave_the_slice_of_a_button_that_names_none_empty() => Topbar.Buttons!.Single(button => button.Id == "help").Slice.ShouldBeNull();
    [Fact] void should_leave_the_policy_of_a_button_that_names_none_empty() => Topbar.Buttons!.Single(button => button.Id == "help").Authorize.ShouldBeNull();
    [Fact] void should_not_report_the_buttons_as_omitted() => _application.ConversionDiagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-SCENE-NAV-001");
}
