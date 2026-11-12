// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Components.for_ExtensionComponentPlaceholder;

public class when_rendering_a_placeholder : Specification
{
    string _html = null!;
    string _escaped = null!;

    void Because()
    {
        _html = ExtensionComponentPlaceholder.Render(new("Orders", "Slack", "ChannelPicker", ExtensionComponentKind.Mount, [], []));
        _escaped = ExtensionComponentPlaceholder.Render(new("Orders", "Sl\"><script>", "X", ExtensionComponentKind.React, [], []));
    }

    [Fact] void should_name_the_component() => _html.ShouldContain("data-stage-extension-component=\"Slack.ChannelPicker\"");
    [Fact] void should_say_how_it_is_hosted() => _html.ShouldContain("data-kind=\"mount\"");
    [Fact] void should_say_it_is_busy_until_it_runs() => _html.ShouldContain("aria-busy=\"true\"");
    [Fact] void should_carry_no_properties_or_settings() => _html.ShouldNotContain("data-settings");
    [Fact] void should_escape_what_it_is_given() => _escaped.ShouldNotContain("<script>");
}
