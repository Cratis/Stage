// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageDevelopment;

public class when_informing_the_host : Specification
{
    string? _docker;
    string? _nothing;
    string? _inheritedDevelopment;
    string? _published;

    void Because()
    {
        _docker = StageDevelopment.EnvironmentFor(true, "Docker");
        _nothing = StageDevelopment.EnvironmentFor(true, null);
        _inheritedDevelopment = StageDevelopment.EnvironmentFor(false, "Development");
        _published = StageDevelopment.EnvironmentFor(false, "Published");
    }

    [Fact] void should_make_the_host_development_when_stage_is_in_development_whatever_it_was_told_before() => _docker.ShouldEqual("Development");
    [Fact] void should_make_the_host_development_when_it_was_told_nothing() => _nothing.ShouldEqual("Development");
    [Fact] void should_not_leave_the_host_in_development_when_stage_is_not() => _inheritedDevelopment.ShouldEqual("Production");
    [Fact] void should_leave_another_environment_alone_when_stage_is_not_in_development() => _published.ShouldBeNull();
}
