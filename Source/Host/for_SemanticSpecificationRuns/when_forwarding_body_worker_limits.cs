// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSpecificationRuns;

public class when_forwarding_body_worker_limits : Specification
{
    ProcessStartInfo _start = null!;

    void Establish() => _start = new();
    void Because() => SemanticBodyWorkerHosting.AddBodyWorkerArguments(_start, new(true, "worker with spaces.dll")
    {
        CacheDirectory = "cache with spaces",
        CallTimeoutMilliseconds = 11,
        CompileTimeoutSeconds = 12,
        MemoryLimitMegabytes = 13
    });

    [Fact]
    void should_keep_every_path_and_limit_in_a_separate_argument() => _start.ArgumentList.ShouldEqual(new[]
    {
        "--body-worker-path", Path.GetFullPath("worker with spaces.dll", AppContext.BaseDirectory), "--body-worker-cache", Path.GetFullPath("cache with spaces"), "--body-worker-dotnet", "dotnet",
        "--body-worker-timeout-ms", "11", "--body-worker-compile-timeout-ms", "12000", "--body-worker-heap-limit-mb", "13",
        "--body-worker-working-set-limit-mb", "26", "--body-worker-max-response-bytes", "1048576", "--body-worker-max-stderr-bytes", "65536"
    });

    [Fact]
    void should_preserve_the_disabled_configuration()
    {
        var disabled = new ProcessStartInfo();
        SemanticBodyWorkerHosting.AddBodyWorkerArguments(disabled, new(false, "worker"));
        disabled.ArgumentList.ShouldBeEmpty();
    }
}
