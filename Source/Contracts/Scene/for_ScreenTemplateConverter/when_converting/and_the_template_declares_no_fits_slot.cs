// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Screens;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenTemplateConverter.when_converting;

public class and_the_template_declares_no_fits_slot : Specification
{
    ScreenTemplateSyntax _syntax = null!;
    ScreenTemplate _result = null!;

    void Establish() => _syntax = new(
        "Dashboard",
        [new SlotSyntax("main", null, SourceLocation.Start)],
        SourceLocation.Start);

    void Because() => _result = ScreenTemplateConverter.Convert(_syntax);

    [Fact] void should_leave_the_slot_it_fits_into_unset() => _result.FitsSlot.ShouldBeNull();
    [Fact] void should_still_carry_its_own_slots() => _result.Slots.Single().Name.ShouldEqual("main");
    [Fact] void should_leave_the_arrangement_unset() => _result.Arrangement.ShouldBeNull();
}
