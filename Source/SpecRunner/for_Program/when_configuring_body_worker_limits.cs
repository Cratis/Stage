// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Execution;
using Xunit;

namespace Cratis.Stage.SpecRunner.for_Program;

public class when_configuring_body_worker_limits : Specification
{
    BodyWorkerOptions _options = null!;

    void Because() => _options = Program.ParseBodyWorkerOptions([
        "--model", "source", "--body-worker-path", "worker with spaces.dll", "--body-worker-cache", "cache with spaces", "--body-worker-dotnet", "custom dotnet",
        "--body-worker-timeout-ms", "11", "--body-worker-compile-timeout-ms", "12", "--body-worker-heap-limit-mb", "13",
        "--body-worker-working-set-limit-mb", "14", "--body-worker-max-response-bytes", "15", "--body-worker-max-stderr-bytes", "16"])!;

    [Fact] void should_preserve_the_worker_path() => _options.WorkerPath.ShouldEqual("worker with spaces.dll");
    [Fact] void should_preserve_the_cache_path() => _options.CacheDirectory.ShouldEqual("cache with spaces");
    [Fact] void should_preserve_the_dotnet_path() => _options.DotnetPath.ShouldEqual("custom dotnet");
    [Fact] void should_preserve_the_evaluation_timeout() => _options.TimeoutMs.ShouldEqual(11);
    [Fact] void should_preserve_the_compilation_timeout() => _options.CompileTimeoutMs.ShouldEqual(12);
    [Fact] void should_preserve_the_heap_limit() => _options.HeapLimitMb.ShouldEqual(13);
    [Fact] void should_preserve_the_working_set_limit() => _options.WorkingSetLimitMb.ShouldEqual(14);
    [Fact] void should_preserve_the_response_limit() => _options.MaxResponseBytes.ShouldEqual(15);
    [Fact] void should_preserve_the_stderr_limit() => _options.MaxStderrBytes.ShouldEqual(16);
    [Fact] void should_preserve_runs_without_worker_configuration() => Program.ParseBodyWorkerOptions(["--model", "source"]).ShouldBeNull();

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("2147483648")]
    [InlineData("NaN")]
    void should_refuse_an_invalid_limit(string value) => Assert.Throws<ArgumentException>(() => Program.ParseBodyWorkerOptions(["--body-worker-path", "worker", "--body-worker-timeout-ms", value]));

    [Fact]
    void should_refuse_a_missing_value() => Assert.Throws<ArgumentException>(() => Program.ParseBodyWorkerOptions(["--body-worker-path"]));
}
#endif
