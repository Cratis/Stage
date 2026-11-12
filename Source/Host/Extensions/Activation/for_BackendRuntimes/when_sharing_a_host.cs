// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Activation.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Activation.for_BackendRuntimes;

public class when_sharing_a_host : a_process_tier
{
    int _hostsWithTwo;
    int _hostsAfterOneFailed;
    int _hostsAfterOneStopped;
    int _hostsAfterBothStopped;
    string _failure = string.Empty;
    bool _otherExited;
    string[] _lines = [];

    async Task Because()
    {
        var log = Path.Combine(_scripts, "host.log");
        var host = Script(
            "host",
            $"""
            while read cmd system port folder; do
              echo "$cmd $system $port" >> {log}
              case "$cmd" in
                load) echo "loaded $system"; case "$system" in Failing*) ( sleep 1; echo "exited $system boom" ) & ;; esac ;;
                unload) echo "unloaded $system" ;;
              esac
            done
            """);
        _options.Execution.Hosts["dotnet"] = new() { Command = host };
        var package = await LoadBackend();
        using var runtime = new SharedHostBackendRuntime(Options, Logger<SharedHostBackendRuntime>());

        var healthy = await runtime.Start(Launch("Mail", package, ExecutionTier.SharedHost, Listen()), CancellationToken.None);
        var failing = await runtime.Start(Launch("FailingBilling", package, ExecutionTier.SharedHost, Listen()), CancellationToken.None);
        _hostsWithTwo = runtime.Hosts;

        _failure = await failing.Exited.WaitAsync(TimeSpan.FromSeconds(20));
        _otherExited = healthy.Exited.IsCompleted;
        _hostsAfterOneFailed = runtime.Hosts;

        await failing.DisposeAsync();
        _hostsAfterOneStopped = runtime.Hosts;
        await healthy.DisposeAsync();
        _hostsAfterBothStopped = runtime.Hosts;
        _lines = await File.ReadAllLinesAsync(log);
    }

    [Fact] void should_load_many_extensions_into_one_host() => _hostsWithTwo.ShouldEqual(1);
    [Fact] void should_ask_the_host_to_load_each_extension() => _lines.Count(line => line.StartsWith("load ", StringComparison.Ordinal)).ShouldEqual(2);
    [Fact] void should_report_the_extension_that_failed_with_the_reason() => _failure.ShouldEqual("boom");
    [Fact] void should_not_stop_the_other_extension_when_one_fails() => _otherExited.ShouldBeFalse();
    [Fact] void should_keep_the_host_while_an_extension_still_runs_in_it() => (_hostsAfterOneFailed, _hostsAfterOneStopped).ShouldEqual((1, 1));
    [Fact] void should_stop_the_host_with_its_last_extension() => _hostsAfterBothStopped.ShouldEqual(0);
}
