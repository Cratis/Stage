// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageDevelopment;

public class when_deciding : Specification
{
    static IConfiguration With(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values.Select(value => new KeyValuePair<string, string?>(value.Key, value.Value))).Build();

    bool _sandbox;
    bool _sandboxTurnedOff;
    bool _published;
    bool _publishedAskedToBeDevelopment;

    void Because()
    {
        _sandbox = StageDevelopment.Decide(true, With());
        _sandboxTurnedOff = StageDevelopment.Decide(true, With((StageDevelopment.Key, "false")));
        _published = StageDevelopment.Decide(false, With());
        _publishedAskedToBeDevelopment = StageDevelopment.Decide(false, With((StageDevelopment.Key, "true")));
    }

    [Fact] void should_treat_the_sandbox_as_development() => _sandbox.ShouldBeTrue();
    [Fact] void should_let_the_sandbox_be_turned_off() => _sandboxTurnedOff.ShouldBeFalse();
    [Fact] void should_never_treat_a_published_application_as_development() => _published.ShouldBeFalse();
    [Fact] void should_not_let_a_setting_make_a_published_application_development() => _publishedAskedToBeDevelopment.ShouldBeFalse();
}
