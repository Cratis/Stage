// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_StageSceneConversion;

public class when_converting_a_model_with_free_screens : given.a_model_with_a_design_system_and_free_screens
{
    void Because() => _scene = StageSceneConversion.Visit(_application);

    [Fact] void should_render_the_free_screen_a_slice_adopts() => _scene.Screens.Select(screen => screen.Name).ShouldContain("InvoiceList");
    [Fact] void should_own_it_by_the_slice_that_adopts_it() => _scene.Screens.Single(screen => screen.Name == "InvoiceList").Owner.ShouldEqual("Invoicing.Paying.ListInvoices");
    [Fact] void should_leave_out_a_free_screen_nobody_adopts() => _scene.Screens.Select(screen => screen.Name).ShouldNotContain("Unused");
}
