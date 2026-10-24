// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedSpecificationArguments;

public class when_running_inline_specifications : Specification
{
    [Fact]
    void should_preserve_the_filter_and_trx_location()
    {
        var arguments = GeneratedSpecificationArguments.Native("App.csproj", ["test", "App.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo", "--filter", "FullyQualifiedName~when_example", "--logger", "trx;LogFileName=run.trx", "--results-directory", "/results", "-m:3", "--", "xUnit.MaxParallelThreads=3"]);
        arguments.ShouldEqual(["run", "--project", "App.csproj", "-c", "Debug", "--no-build", "--no-restore", "--", "--specifications", "-maxThreads", "3", "-filterVSTest", "FullyQualifiedName~when_example", "-result-trx", "/results/run.trx"]);
    }

    [Fact]
    void should_run_the_discovered_project_without_a_filter()
    {
        var arguments = GeneratedSpecificationArguments.Native("App.csproj", ["test", "-c", "Debug", "--no-build"]);
        arguments.ShouldEqual(["run", "--project", "App.csproj", "-c", "Debug", "--no-build", "--", "--specifications", "-maxThreads", "3"]);
    }

    [Fact] void should_refuse_an_unknown_option() => Assert.Throws<ArgumentException>(() => GeneratedSpecificationArguments.Native("App.csproj", ["test", "--unknown"]));
    [Fact] void should_refuse_an_unrecognized_logger() => Assert.Throws<ArgumentException>(() => GeneratedSpecificationArguments.Native("App.csproj", ["test", "--logger", "unknown"]));
}
