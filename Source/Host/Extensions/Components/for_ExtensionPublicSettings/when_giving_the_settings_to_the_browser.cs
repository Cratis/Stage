// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Components.for_ExtensionPublicSettings;

public class when_giving_the_settings_to_the_browser : Specification
{
    ISystemSettings _values = null!;
    ExtensionPublicSettings _settings = null!;
    IReadOnlyDictionary<string, string> _result = null!;

    void Establish()
    {
        _values = Substitute.For<ISystemSettings>();
        _values.Resolve("Slack", "workspace").Returns("acme");
        _values.Resolve("Slack", "unset").Returns((string?)null);
        _values.Resolve("Slack", "botToken").Returns("xoxb-secret");
        _values.Resolve("Slack", "undeclared").Returns("surprise");
        _values.Resolve("Mail", "host").Returns("smtp.example.com");
        _settings = new ExtensionPublicSettings(_values, new SystemSettingDeclarations([
            new("Slack", "workspace", false),
            new("Slack", "botToken", true),
            new("Slack", "unset", false),
            new("Mail", "host", false)
        ]));
    }

    void Because() => _result = _settings.Of("Slack");

    [Fact] void should_give_the_public_setting() => _result["workspace"].ShouldEqual("acme");
    [Fact] void should_give_nothing_but_the_public_settings_that_have_a_value() => _result.Count.ShouldEqual(1);
    [Fact] void should_never_include_a_secret() => _result.ContainsKey("botToken").ShouldBeFalse();
    [Fact] void should_never_include_the_value_of_a_secret() => _result.Values.ShouldNotContain("xoxb-secret");
    [Fact] void should_never_read_a_secret() => _values.DidNotReceive().Resolve("Slack", "botToken");
    [Fact] void should_never_include_what_nobody_declared() => _result.ContainsKey("undeclared").ShouldBeFalse();
    [Fact] void should_keep_to_the_system() => _result.ContainsKey("host").ShouldBeFalse();
}
