// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace Cratis.Stage.Contracts.Execution;

/// <summary>Owns the redirected child and its bounded streams.</summary>
/// <param name="options">The launch settings and resource bounds.</param>
/// <param name="role">The worker process role.</param>
internal sealed class BodyWorkerProcess(BodyWorkerOptions options, string role) : IAsyncDisposable
{
    Process? _process;
    Task<string>? _stderr;

    internal int? ProcessId => _process?.Id;

    internal bool ExceededMemory
    {
        get => field || (_stderr?.IsCompletedSuccessfully == true && MemoryMarker(_stderr.Result));
        private set;
    }

    public async ValueTask DisposeAsync()
    {
        var process = _process;
        _process = null;
        if (process is null) return;
        try
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            using var reap = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await process.WaitForExitAsync(reap.Token).ConfigureAwait(false);
            if (_stderr is not null) await _stderr.WaitAsync(reap.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            process.StandardError.Close();
        }
        finally
        {
            process.Dispose();
        }
    }

    internal static void ConfigureEnvironment(ProcessStartInfo info, string cacheDirectory, Func<string, string?> read)
    {
        info.Environment.Clear();
        foreach (var name in new[] { "DOTNET_ROOT", "DOTNET_ROOT_X64", "DOTNET_ROOT_ARM64", "DOTNET_ROOT_X86", "DOTNET_ROOT_ARM", "DOTNET_ROOT(x86)", "SystemRoot" })
        {
            if (read(name) is { Length: > 0 } value) info.Environment[name] = value;
        }
        foreach (var name in new[] { "HOME", "TMPDIR", "TEMP", "TMP" }) info.Environment[name] = cacheDirectory;
    }

    internal async Task StartAsync(CancellationToken token)
    {
        if (_process is not null) return;
        BodyWorkerCacheDirectory.Prepare(options.CacheDirectory);
        var info = StartInfo();
        _process = Process.Start(info) ?? throw new IOException("Body worker did not start.");
        _stderr = DrainStderrAsync(_process.StandardError);
        var line = await BodyWorkerPipe.ReadLineAsync(_process.StandardOutput, 4096, token).ConfigureAwait(false);
        var hello = JsonSerializer.Deserialize<BodyWorkerHello>(line, BodyWorkerMessages.Json);
        if (hello is not { Kind: "hello", ProtocolVersion: BodyWorkerMessages.ProtocolVersion } || string.IsNullOrWhiteSpace(hello.WorkerVersion))
        {
            throw new IOException("Body worker handshake mismatch.");
        }
    }

    internal async Task<BodyWorkerResponse> ExchangeAsync(BodyWorkerRequest request, CancellationToken token)
    {
        var process = _process!;
        await process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(request, BodyWorkerMessages.Json).AsMemory(), token).ConfigureAwait(false);
        await process.StandardInput.FlushAsync(token).ConfigureAwait(false);
        var monitor = CancellationTokenSource.CreateLinkedTokenSource(token);
        var memory = WatchMemoryAsync(process, monitor.Token);
        try
        {
            var line = await BodyWorkerPipe.ReadLineAsync(process.StandardOutput, options.MaxResponseBytes, token).ConfigureAwait(false);
            return JsonSerializer.Deserialize<BodyWorkerResponse>(line, BodyWorkerMessages.Json) ?? throw new IOException("Empty worker response.");
        }
        finally
        {
            await monitor.CancelAsync().ConfigureAwait(false);
            await memory.ConfigureAwait(false);
            monitor.Dispose();
        }
    }

    static bool MemoryMarker(string text) => text.Contains(BodyWorkerMessages.OutOfMemoryMarker, StringComparison.Ordinal) || text.Contains("Out of memory.", StringComparison.OrdinalIgnoreCase);

    ProcessStartInfo StartInfo()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.HeapLimitMb);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.WorkingSetLimitMb);
        var info = new ProcessStartInfo(options.Launch?.FileName ?? options.DotnetPath)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetFullPath(options.CacheDirectory)
        };
        foreach (var argument in options.Launch?.Arguments ?? [Path.GetFullPath(options.WorkerPath)]) info.ArgumentList.Add(argument);
        info.ArgumentList.Add("--role");
        info.ArgumentList.Add(role);
        ConfigureEnvironment(info, info.WorkingDirectory, Environment.GetEnvironmentVariable);
        var heapMb = role == "compile" ? 1024 : options.HeapLimitMb;
        info.Environment["DOTNET_GCHeapHardLimit"] = ((long)heapMb * 1024 * 1024).ToString("X", CultureInfo.InvariantCulture);
        info.Environment["DOTNET_gcServer"] = "0";
        info.Environment["DOTNET_EnableDiagnostics"] = "0";
        return info;
    }

    async Task WatchMemoryAsync(Process process, CancellationToken token)
    {
        var limit = role == "compile" ? 4096 : options.WorkingSetLimitMb;
        try
        {
            while (!process.HasExited)
            {
                process.Refresh();
                if (process.WorkingSet64 > (long)limit * 1024 * 1024)
                {
                    ExceededMemory = true;
                    process.Kill(entireProcessTree: true);
                    return;
                }
                await Task.Delay(100, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    async Task<string> DrainStderrAsync(StreamReader reader)
    {
        var buffer = new char[1024];
        var text = new BodyWorkerErrorBuffer(options.MaxStderrBytes);
        try
        {
            int read;
            while ((read = await reader.ReadAsync(buffer).ConfigureAwait(false)) != 0)
            {
                text.Append(buffer.AsSpan(0, read));
            }
        }
        catch (IOException)
        {
        }
        return text.ToString();
    }
}
