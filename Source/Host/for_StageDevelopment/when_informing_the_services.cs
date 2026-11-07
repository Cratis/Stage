// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Microsoft.Extensions.FileProviders;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_StageDevelopment;

public class when_informing_the_services : Specification
{
    static IWebHostEnvironment Environment(string name)
    {
        var environment = Substitute.For<IWebHostEnvironment>();
        environment.EnvironmentName.Returns(name);
        environment.ApplicationName.Returns("Stage");
        environment.ContentRootPath.Returns("/stage");
        environment.ContentRootFileProvider.Returns(Substitute.For<IFileProvider>());
        return environment;
    }

    IWebHostEnvironment _sandbox = null!;
    IHostEnvironment _hostSeen = null!;
    IHostEnvironment _published = null!;
    IHostEnvironment _handedDevelopment = null!;

    void Because()
    {
        var sandbox = new ServiceCollection();
        StageDevelopment.Inform(sandbox, Environment("Docker"), true);
        _sandbox = sandbox.BuildServiceProvider().GetRequiredService<IWebHostEnvironment>();
        _hostSeen = sandbox.BuildServiceProvider().GetRequiredService<IHostEnvironment>();

        var published = new ServiceCollection();
        StageDevelopment.Inform(published, Environment("Published"), false);
        _published = published.BuildServiceProvider().GetService<IHostEnvironment>()!;

        var handed = new ServiceCollection();
        StageDevelopment.Inform(handed, Environment("Development"), false);
        _handedDevelopment = handed.BuildServiceProvider().GetRequiredService<IHostEnvironment>();
    }

    [Fact] void should_answer_development_for_a_development_stage() => _hostSeen.IsDevelopment().ShouldBeTrue();
    [Fact] void should_answer_the_same_through_the_web_host_environment() => _sandbox.IsDevelopment().ShouldBeTrue();
    [Fact] void should_keep_everything_else_the_process_started_with() => _sandbox.ContentRootPath.ShouldEqual("/stage");
    [Fact] void should_not_answer_for_a_stage_that_is_not_development_and_was_not_handed_development() => _published.ShouldBeNull();
    [Fact] void should_answer_production_for_a_stage_that_is_not_development_but_was_handed_it() => _handedDevelopment.IsProduction().ShouldBeTrue();
}
