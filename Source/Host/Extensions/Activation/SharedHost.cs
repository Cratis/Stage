// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;

namespace Cratis.Stage.Host.Extensions.Activation;

/// <summary>
/// The process of a shared runtime host, and the conversation with it. One host loads many extensions and isolates each, so an extension that fails is reported by name and the others keep running.
/// </summary>
/// <remarks>
/// The conversation is one line each way. Stage writes <c lang="text">load &lt;system&gt; &lt;port&gt; &lt;folder&gt;</c> and <c lang="text">unload &lt;system&gt;</c>. The host writes
/// <c lang="text">loaded &lt;system&gt;</c>, <c lang="text">failed &lt;system&gt; &lt;reason&gt;</c>, <c lang="text">unloaded &lt;system&gt;</c> and, when an extension it hosts fails later,
/// <c lang="text">exited &lt;system&gt; &lt;reason&gt;</c>. Any other line is the host's log.
/// </remarks>
internal sealed class SharedHost : IDisposable
{
    readonly Process _process;
    readonly ILogger _logger;
    readonly ConcurrentDictionary<string, TaskCompletionSource<(bool Ok, string Reason)>> _replies = new(StringComparer.Ordinal);
    readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _exits = new(StringComparer.Ordinal);
    readonly TaskCompletionSource<string> _gone = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly SemaphoreSlim _writing = new(1, 1);

    SharedHost(Process process, ILogger logger)
    {
        _process = process;
        _logger = logger;
    }

    /// <summary>
    /// Gets how many extensions the host holds.
    /// </summary>
    public int Loaded { get; private set; }

    /// <summary>
    /// Gets whether the process still runs.
    /// </summary>
    public bool IsAlive => !_gone.Task.IsCompleted;

    /// <summary>
    /// Starts a host.
    /// </summary>
    /// <param name="options">How to start it.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The <see cref="SharedHost"/>.</returns>
    /// <exception cref="BackendStartException">The host did not start.</exception>
    public static SharedHost Start(SharedHostOptions options, ILogger logger)
    {
        var info = new ProcessStartInfo(options.Command)
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in options.Arguments) info.ArgumentList.Add(argument);

        var process = new Process { StartInfo = info };
        var host = new SharedHost(process, logger);
        process.OutputDataReceived += (_, line) => { if (line.Data is not null) host.Read(line.Data); };
        process.ErrorDataReceived += (_, line) => { if (line.Data is not null) ActivationLog.BackendOutput(logger, "host", line.Data); };
        process.Exited += (_, _) => host.Gone($"The shared host exited with code {process.ExitCode}.");
        process.EnableRaisingEvents = true;
        try
        {
            if (!process.Start()) throw new BackendStartException($"{options.Command} did not start.");
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            process.Dispose();
            throw new BackendStartException($"{options.Command} could not be started: {exception.Message}", exception);
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return host;
    }

    /// <summary>
    /// Asks the host to load an extension.
    /// </summary>
    /// <param name="system">The system.</param>
    /// <param name="port">The port the extension must listen on.</param>
    /// <param name="folder">The folder the backend is unpacked to.</param>
    /// <param name="timeout">How long the host may take.</param>
    /// <param name="cancellationToken">Cancelled when the caller stops waiting.</param>
    /// <returns>A task that completes with the task that completes, with the reason, when the extension is gone.</returns>
    /// <exception cref="BackendStartException">The host refused the extension or did not answer.</exception>
    public async Task<Task<string>> Load(string system, int port, string folder, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var exit = _exits.GetOrAdd(system, _ => new(TaskCreationOptions.RunContinuationsAsynchronously));
        var (ok, reason) = await Ask($"load {system} {port.ToString(System.Globalization.CultureInfo.InvariantCulture)} {folder}", system, timeout, cancellationToken);
        if (!ok)
        {
            _exits.TryRemove(system, out _);
            throw new BackendStartException($"The shared host did not load {system}: {reason}");
        }

        Loaded++;
        return exit.Task;
    }

    /// <summary>
    /// Asks the host to unload an extension.
    /// </summary>
    /// <param name="system">The system.</param>
    /// <param name="timeout">How long the host may take.</param>
    /// <returns>A task that completes when the host answered or the time is up.</returns>
    public async Task Unload(string system, TimeSpan timeout)
    {
        if (IsAlive)
        {
            using var limit = new CancellationTokenSource(timeout);
            try
            {
                await Ask($"unload {system}", system, timeout, limit.Token);
            }
            catch (BackendStartException)
            {
                // A host that does not answer is stopped with the last of its extensions.
            }
            catch (OperationCanceledException)
            {
                // As above.
            }
        }

        if (_exits.TryRemove(system, out var exit)) exit.TrySetResult("The backend was stopped.");
        Loaded--;
    }

    /// <summary>
    /// Stops the process, asking first by closing its input.
    /// </summary>
    /// <param name="timeout">How long it may take before it is killed.</param>
    /// <returns>A task that completes when the process is gone.</returns>
    public async Task Stop(TimeSpan timeout)
    {
        if (!_process.HasExited)
        {
            try
            {
                _process.StandardInput.Close();
                using var limit = new CancellationTokenSource(timeout);
                await _process.WaitForExitAsync(limit.Token);
            }
            catch (Exception exception) when (exception is OperationCanceledException or IOException or InvalidOperationException)
            {
                try
                {
                    _process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                    // It exited in the meantime.
                }
            }
        }

        Gone("The shared host was stopped.");
        Dispose();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _process.Dispose();
        _writing.Dispose();
    }

    async Task<(bool Ok, string Reason)> Ask(string line, string system, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var reply = new TaskCompletionSource<(bool Ok, string Reason)>(TaskCreationOptions.RunContinuationsAsynchronously);
        _replies[system] = reply;
        try
        {
            if (!IsAlive) return (false, "The shared host is not running.");
            await _writing.WaitAsync(cancellationToken);
            try
            {
                await _process.StandardInput.WriteLineAsync(line.AsMemory(), cancellationToken);
                await _process.StandardInput.FlushAsync(cancellationToken);
            }
            catch (IOException exception)
            {
                return (false, exception.Message);
            }
            finally
            {
                _writing.Release();
            }

            return await reply.Task.WaitAsync(timeout, cancellationToken);
        }
        catch (TimeoutException)
        {
            return (false, $"No answer within {timeout}.");
        }
        finally
        {
            _replies.TryRemove(system, out _);
        }
    }

    void Read(string line)
    {
        var parts = line.Split(' ', 3, StringSplitOptions.None);
        if (parts.Length >= 2)
        {
            var reason = parts.Length > 2 ? parts[2] : string.Empty;
            switch (parts[0])
            {
                case "loaded" or "unloaded":
                    if (_replies.TryGetValue(parts[1], out var accepted)) accepted.TrySetResult((true, string.Empty));
                    return;
                case "failed":
                    if (_replies.TryGetValue(parts[1], out var refused)) refused.TrySetResult((false, reason));
                    return;
                case "exited":
                    if (_exits.TryGetValue(parts[1], out var exit)) exit.TrySetResult(reason.Length > 0 ? reason : "The extension exited.");
                    return;
            }
        }

        ActivationLog.BackendOutput(_logger, "host", line);
    }

    void Gone(string reason)
    {
        if (!_gone.TrySetResult(reason)) return;
        foreach (var reply in _replies.Values) reply.TrySetResult((false, reason));
        foreach (var exit in _exits.Values) exit.TrySetResult(reason);
    }
}
