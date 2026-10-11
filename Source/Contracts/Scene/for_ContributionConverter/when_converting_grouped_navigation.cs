// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Elements;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ContributionConverter;

public class when_converting_grouped_navigation : Specification
{
    ExternalComponent _content = null!;

    void Because() => _content = (ExternalComponent)ContributionConverter.Convert(
        new ContributionSyntax("Navigation", new ScreenNavigateSyntax("WorkItemList", null, SourceLocation.Start), "Work items", 10, SourceLocation.Start) { Group = "Tracking" },
        "navigation").Content;

    [Fact] void should_carry_the_group_in_the_property_read_by_scene() => _content.Properties["group"].ShouldEqual("Tracking");
    [Fact] void should_preserve_the_screen_target() => _content.Properties["targetScreen"].ShouldEqual("WorkItemList");
}
