// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Cratis.Stage.Contracts.Execution;

/// <summary>Owns the redirected child and its bounded streams.</summary>
internal sealed class BodyWorkerProcess(BodyWorkerOptions options) : IAsyncDisposable
{
    Process? _process;
    Task<string>? _stderr;

    internal int? ProcessId => _process?.Id;

    internal bool ExceededMemory
    {
        get => field || (_stderr?.IsCompletedSuccessfully == true && _stderr.Result.Contains(BodyWorkerMessages.OutOfMemoryMarker, StringComparison.Ordinal));
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
            await process.WaitForExitAsync();
            if (_stderr is not null) await _stderr;
        }
        finally
        {
            process.Dispose();
        }
    }

    internal async Task StartAsync(CancellationToken token)
    {
        Directory.CreateDirectory(options.CacheDirectory);
        var info = new ProcessStartInfo(options.DotnetPath)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetFullPath(options.CacheDirectory)
        };
        info.ArgumentList.Add(Path.GetFullPath(options.WorkerPath));
        info.Environment.Clear();
        info.Environment["DOTNET_GCHeapHardLimit"] = ((long)options.HeapLimitMb * 1024 * 1024).ToString("X", CultureInfo.InvariantCulture);
        info.Environment["DOTNET_EnableDiagnostics"] = "0";
        _process = Process.Start(info) ?? throw new IOException("Body worker did not start.");
        _stderr = DrainStderrAsync(_process.StandardError);
        var hello = JsonSerializer.Deserialize<BodyWorkerHello>(await BodyWorkerPipe.ReadLineAsync(_process.StandardOutput, 4096, token), BodyWorkerMessages.Json);
        if (hello is not { Kind: "hello", ProtocolVersion: BodyWorkerMessages.ProtocolVersion } || string.IsNullOrWhiteSpace(hello.WorkerVersion))
        {
            throw new IOException("Body worker handshake mismatch.");
        }
    }

    internal async Task<BodyWorkerResponse> ExchangeAsync(BodyWorkerRequest request, CancellationToken token)
    {
        var process = _process!;
        await process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(request, BodyWorkerMessages.Json).AsMemory(), token);
        await process.StandardInput.FlushAsync(token);
        var monitor = CancellationTokenSource.CreateLinkedTokenSource(token);
        var memory = WatchMemoryAsync(process, monitor.Token);
        try
        {
            var line = await BodyWorkerPipe.ReadLineAsync(process.StandardOutput, options.MaxResponseBytes, token);
            return JsonSerializer.Deserialize<BodyWorkerResponse>(line, BodyWorkerMessages.Json) ?? throw new IOException("Empty worker response.");
        }
        catch (IOException)
        {
            if (process.HasExited && _stderr is not null) await _stderr;
            throw;
        }
        finally
        {
            await monitor.CancelAsync();
            await memory;
            monitor.Dispose();
        }
    }

    async Task WatchMemoryAsync(Process process, CancellationToken token)
    {
        try
        {
            while (!process.HasExited)
            {
                process.Refresh();
                if (process.WorkingSet64 > (long)options.HeapLimitMb * 1024 * 1024 * 4)
                {
                    ExceededMemory = true;
                    process.Kill(entireProcessTree: true);
                    return;
                }
                await Task.Delay(25, token);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    async Task<string> DrainStderrAsync(StreamReader reader)
    {
        var buffer = new char[1024];
        var text = new StringBuilder();
        int read;
        while ((read = await reader.ReadAsync(buffer)) != 0)
        {
            var retained = Math.Min(read, Math.Max(0, options.MaxStderrBytes - text.Length));
            text.Append(buffer, 0, retained);
        }
        return text.ToString();
    }
}
