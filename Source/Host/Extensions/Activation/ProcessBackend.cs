// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Stage.Host.Extensions.Activation;

/// <summary>
/// A backend that is a process Stage started.
/// </summary>
public sealed class ProcessBackend : StartedBackend
{
    readonly Process _process;
    readonly TaskCompletionSource<string> _exited = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly Func<TimeSpan, Task>? _stopper;
    readonly TimeSpan _stopTimeout;
    readonly Action? _cleanup;

    ProcessBackend(Process process, Func<TimeSpan, Task>? stopper, TimeSpan stopTimeout, Action? cleanup)
    {
        _process = process;
        _stopper = stopper;
        _stopTimeout = stopTimeout;
        _cleanup = cleanup;
        process.Exited += (_, _) => Complete($"The process exited with code {SafeExitCode(process)}.");
        process.EnableRaisingEvents = true;
        if (process.HasExited) Complete($"The process exited with code {SafeExitCode(process)}.");
    }

    /// <inheritdoc/>
    public override Task<string> Exited => _exited.Task;

    /// <summary>
    /// Starts a process.
    /// </summary>
    /// <param name="info">What to start. Output is read and passed to <paramref name="output"/>.</param>
    /// <param name="stopTimeout">How long a stop may take before the process is killed.</param>
    /// <param name="output">Receives each line the process writes.</param>
    /// <param name="stopper">Stops the process more gracefully than killing it, when there is a way to. It is given the time it has.</param>
    /// <param name="cleanup">Runs once the process is gone.</param>
    /// <returns>The <see cref="ProcessBackend"/>.</returns>
    /// <exception cref="BackendStartException">The process did not start.</exception>
    public static ProcessBackend Start(ProcessStartInfo info, TimeSpan stopTimeout, Action<string> output, Func<TimeSpan, Task>? stopper = null, Action? cleanup = null)
    {
        info.RedirectStandardOutput = true;
        info.RedirectStandardError = true;
        info.UseShellExecute = false;
        var process = new Process { StartInfo = info };
        process.OutputDataReceived += (_, line) => { if (line.Data is not null) output(line.Data); };
        process.ErrorDataReceived += (_, line) => { if (line.Data is not null) output(line.Data); };
        try
        {
            if (!process.Start()) throw new BackendStartException($"{info.FileName} did not start.");
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            process.Dispose();
            throw new BackendStartException($"{info.FileName} could not be started: {exception.Message}", exception);
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return new(process, stopper, stopTimeout, cleanup);
    }

    /// <inheritdoc/>
    public override async ValueTask DisposeAsync()
    {
        if (!_process.HasExited)
        {
            try
            {
                if (_stopper is not null) await _stopper(_stopTimeout);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Killing it below is what is left.
                Debug.WriteLine(exception);
            }

            if (!_process.HasExited)
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

            using var limit = new CancellationTokenSource(_stopTimeout);
            try
            {
                await _process.WaitForExitAsync(limit.Token);
            }
            catch (OperationCanceledException)
            {
                // A process that cannot be killed is reported by its still being there.
            }
        }

        Complete("The backend was stopped.");
        _process.Dispose();
        _cleanup?.Invoke();
    }

    static string SafeExitCode(Process process)
    {
        try
        {
            return process.ExitCode.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (InvalidOperationException)
        {
            return "unknown";
        }
    }

    void Complete(string reason) => _exited.TrySetResult(reason);
}
