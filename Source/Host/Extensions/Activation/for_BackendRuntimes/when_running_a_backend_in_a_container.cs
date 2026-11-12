// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Activation.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Activation.for_BackendRuntimes;

public class when_running_a_backend_in_a_container : a_process_tier
{
    string[] _commands = [];
    BackendLaunch _launch = null!;
    bool _runningWhileActive;

    async Task Because()
    {
        var log = Path.Combine(_scripts, "docker.log");
        var docker = Script("docker", $"echo \"$@\" >> {log}\nif [ \"$1\" = run ]; then exec sleep 300; fi");
        _options.Execution.Containers["Mail"] = new() { Command = docker, Image = "ghcr.io/acme/mail:1", Arguments = ["--memory", "256m"] };
        var package = await LoadBackend();
        _launch = Launch("Mail", package, ExecutionTier.Container, Listen());

        var backend = await new ContainerBackendRuntime(Options, Logger<ContainerBackendRuntime>()).Start(_launch, CancellationToken.None);
        await Wait(log);
        _runningWhileActive = !backend.Exited.IsCompleted;
        await backend.DisposeAsync();
        _commands = [.. (await File.ReadAllLinesAsync(log)).Where(line => line.Length > 0)];
    }

    [Fact] void should_run_the_configured_image() => _commands[0].ShouldEqual(string.Join(' ', ContainerBackendRuntime.RunArguments(_launch, _options.Execution.Containers["Mail"])));
    [Fact] void should_publish_the_port_stage_connects_to() => _commands[0].ShouldContain($"--publish 127.0.0.1:{_launch.Address.Port}:{ContainerBackendRuntime.ContainerPort}");
    [Fact] void should_tell_the_backend_to_listen_inside_the_container() => _commands[0].ShouldContain($"{EnvironmentNames.Listen}=0.0.0.0:{ContainerBackendRuntime.ContainerPort}");
    [Fact] void should_pass_the_deployments_own_arguments_before_the_image() => _commands[0].ShouldContain("--memory 256m ghcr.io/acme/mail:1");
    [Fact] void should_have_it_running_once_it_listens() => _runningWhileActive.ShouldBeTrue();
    [Fact] void should_stop_the_container_by_name() => _commands[^1].ShouldEqual($"stop --time 10 {ContainerBackendRuntime.NameOf(_launch)}");
}
