// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_NavigationConverter;

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
