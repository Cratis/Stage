// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Execution;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSpecificationRuns;

public class when_configuring_a_body_worker : Specification
{
    [Fact]
    void should_use_the_shipped_worker_and_default_limits()
    {
        var settings = SemanticBodyWorkerOptions.From(new ConfigurationBuilder().Build());
        settings.Enabled.ShouldBeTrue();
        settings.WorkerPath.ShouldEqual(Path.Combine(AppContext.BaseDirectory, "bodyworker", "Cratis.Stage.BodyWorker.dll"));
        settings.CacheDirectory.ShouldEqual(BodyWorkerCacheDirectory.Default);
        settings.CallTimeoutMilliseconds.ShouldEqual(2000);
        settings.CompileTimeoutSeconds.ShouldEqual(120);
        settings.MemoryLimitMegabytes.ShouldEqual(256);
        settings.Worker().WorkingSetLimitMb.ShouldEqual(512);
    }

    [Fact]
    void should_forward_deployment_overrides_to_the_runner()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Stage:BodyWorker:Enabled"] = "false",
            ["Stage:BodyWorker:WorkerPath"] = "alternate/worker.dll",
            ["Stage:BodyWorker:CacheDirectory"] = "alternate/cache",
            ["Stage:BodyWorker:CallTimeoutMilliseconds"] = "500",
            ["Stage:BodyWorker:CompileTimeoutSeconds"] = "30",
            ["Stage:BodyWorker:MemoryLimitMegabytes"] = "64"
        }).Build();
        var settings = SemanticSpecificationProcessOptions.FromConfiguration(configuration).BodyWorker!;
        settings.Enabled.ShouldBeFalse();
        settings.Worker().WorkerPath.ShouldEqual(Path.GetFullPath("alternate/worker.dll", AppContext.BaseDirectory));
        settings.Worker().CacheDirectory.ShouldEqual(Path.GetFullPath("alternate/cache"));
        settings.Worker().TimeoutMs.ShouldEqual(500);
        settings.Worker().CompileTimeoutMs.ShouldEqual(30000);
        settings.Worker().HeapLimitMb.ShouldEqual(64);
        settings.Worker().WorkingSetLimitMb.ShouldEqual(128);
    }

    [Theory]
    [InlineData("-1", 50, 5, 32)]
    [InlineData("2147483647", 30000, 600, 4096)]
    void should_bound_the_configured_limits(string value, int timeout, int compile, int memory)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Stage:BodyWorker:CallTimeoutMilliseconds"] = value,
            ["Stage:BodyWorker:CompileTimeoutSeconds"] = value,
            ["Stage:BodyWorker:MemoryLimitMegabytes"] = value
        }).Build();
        var settings = SemanticBodyWorkerOptions.From(configuration);
        settings.CallTimeoutMilliseconds.ShouldEqual(timeout);
        settings.CompileTimeoutSeconds.ShouldEqual(compile);
        settings.MemoryLimitMegabytes.ShouldEqual(memory);
    }
}
