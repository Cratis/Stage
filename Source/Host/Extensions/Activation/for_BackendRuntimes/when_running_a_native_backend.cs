// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Activation.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Activation.for_BackendRuntimes;

public class when_running_a_native_backend : a_process_tier
{
    string _seen = string.Empty;
    Uri _address = null!;
    bool _exitedWhileRunning;
    string _exitReason = string.Empty;
    bool _unpackedFolderRemoved;
    string _packageFolder = string.Empty;

    async Task Because()
    {
        var seen = Path.Combine(_scripts, "seen.txt");
        var script = $"echo \"$STAGE_EXTENSION_LISTEN|$STAGE_EXTENSION_SYSTEM|$PWD\" > {seen}\nexec sleep 300";
        var package = await LoadBackend("native", entry: "server.sh", content: System.Text.Encoding.UTF8.GetBytes("#!/bin/sh\n" + script + "\n"));
        _address = Listen();
        _packageFolder = Path.Combine("backends", package.Id[..16]);

        var backend = await new NativeBackendRuntime(Options, Logger<NativeBackendRuntime>()).Start(Launch("Mail", package, ExecutionTier.Native, _address), CancellationToken.None);
        _seen = (await Wait(seen)).Trim();
        _exitedWhileRunning = backend.Exited.IsCompleted;

        await backend.DisposeAsync();
        _exitReason = await backend.Exited;
        var backends = _options.Execution.BackendsDirectory;
        _unpackedFolderRemoved = !Directory.Exists(backends) || Directory.GetDirectories(backends).Length == 0;
    }

    [Fact] void should_tell_the_backend_where_to_listen() => _seen.Split('|')[0].ShouldEqual($"{_address.Host}:{_address.Port}");
    [Fact] void should_tell_the_backend_which_system_it_serves() => _seen.Split('|')[1].ShouldEqual("Mail");
    [Fact] void should_run_it_in_the_folder_it_was_unpacked_to() => _seen.Split('|')[2].Contains(_packageFolder, StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_have_it_running_once_it_listens() => _exitedWhileRunning.ShouldBeFalse();
    [Fact] void should_stop_the_process() => _exitReason.ShouldNotBeNull();
    [Fact] void should_remove_what_it_unpacked() => _unpackedFolderRemoved.ShouldBeTrue();
}
