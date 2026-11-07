// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Outlets;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_NavigationConverter;

// Decision 0098 (#868, Scene#215): a button beside the layout's topbar belongs to the application. Screenplay 6 declares those buttons as the
// actions outlet topbarActions; Scene draws the buttons of a navigation outlet from <outlet>Buttons.
public class when_the_topbar_declares_application_toolbar_buttons : given.a_translated_application
{
    const string Shell =
        """
        policy IsBuyer
          require role "buyer"

        layout AppShell
          category application
          content page routed primary

          outlet topbar of navigation
            default shop title "Shop" target Shop
          outlet topbarActions of actions
            default place title "Place order" slice Shop.Orders.PlaceOrder emphasis primary icon plus
              authorize IsBuyer
            default help title "Help"

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

    void Because() => Translate(Shell);

    Outlet Buttons => _application.Navigation.Application.Outlets.Single(outlet => outlet.Name == "topbarButtons");
    OutletButton Place => (OutletButton)Buttons.Defaults.Single(button => button.Id == "place");
    OutletButton Help => (OutletButton)Buttons.Defaults.Single(button => button.Id == "help");

    [Fact] void should_still_convert_the_navigation_outlet_beside_the_buttons() => _application.Navigation.Application.Outlets.Single(outlet => outlet.Name == "topbar").Defaults.Single().Id.ShouldEqual("shop");
    [Fact] void should_carry_the_buttons_as_the_outlet_scene_draws_beside_the_topbar() => Buttons.Kind.ShouldEqual(OutletKind.Actions);
    [Fact] void should_carry_every_button() => Buttons.Defaults.Select(button => button.Id).ShouldContainOnly("place", "help");
    [Fact] void should_carry_the_title() => Place.Title.ShouldEqual("Place order");
    [Fact] void should_write_the_slice_as_the_qualified_id() => Place.Slice.ShouldEqual("Shop.Orders.PlaceOrder");
    [Fact] void should_carry_the_emphasis() => Place.Emphasis.ShouldEqual(Cratis.Scene.Model.Toolbar.ToolbarEmphasis.Primary);
    [Fact] void should_carry_the_icon() => Place.Icon.ShouldEqual("plus");
    [Fact] void should_carry_the_policy() => Place.Authorize.ShouldEqual("IsBuyer");
    [Fact] void should_leave_the_slice_of_a_button_that_names_none_empty() => Help.Slice.ShouldBeNull();
    [Fact] void should_leave_the_policy_of_a_button_that_names_none_empty() => Help.Authorize.ShouldBeNull();
    [Fact] void should_not_keep_the_screenplay_name() => _application.Navigation.Application.Outlets.Select(outlet => outlet.Name).ShouldNotContain("topbarActions");
}
