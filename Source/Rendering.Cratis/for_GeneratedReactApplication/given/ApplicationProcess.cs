// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;
using Cratis.Stage.Rendering.Cratis.given;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication.given;

/// <summary>
/// Runs a build tool in a generated application and keeps its exit code with its output.
/// </summary>
public static class ApplicationProcess
{
    static readonly TimeSpan _teardownBudget = TimeSpan.FromSeconds(10);
    static readonly TimeSpan _drainBudget = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Runs a tool to completion, or kills it with its whole process tree when it runs past the deadline.
    /// </summary>
    /// <param name="directory">The working directory.</param>
    /// <param name="timeout">How long the tool may run.</param>
    /// <param name="fileName">The tool.</param>
    /// <param name="arguments">The tool's arguments.</param>
    /// <returns>The exit code and the combined output. A tool that could not start or timed out has exit code -1.</returns>
    public static Task<ProcessResult> Run(string directory, TimeSpan timeout, string fileName, params string[] arguments) =>
        Execute(new(new(directory, timeout, null, fileName, arguments), false, CancellationToken.None));

    /// <summary>
    /// Runs a tool like <see cref="Run(string, TimeSpan, string, string[])"/>, streams its command, process id and output to a log as it happens,
    /// counts the wait for a build slot against the deadline and stops when cancelled.
    /// </summary>
    /// <param name="tool">The tool to run, where, for how long and where its log goes.</param>
    /// <param name="cancellationToken">Cancels the wait for a slot and the tool.</param>
    /// <returns>The exit code and the combined output. A tool that could not start, timed out, was cancelled or could not be confirmed stopped has exit code -1.</returns>
    public static Task<ProcessResult> RunLogged(LoggedTool tool, CancellationToken cancellationToken) =>
        Execute(new(new(tool.Directory, tool.Timeout, tool.LogPath, tool.FileName, tool.Arguments), true, cancellationToken));

    static async Task<ProcessResult> Execute(ProcessRequest request)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(request.CancellationToken);
        if (request.DeadlineIncludesQueue)
        {
            deadline.CancelAfter(request.Timeout);
        }

        // Bounded so a local run does not start every generated application's build at once.
        var slot = await EnterSlot(request, deadline.Token);
        if (slot is null)
        {
            return new(-1, $"{request.Command} did not start: {StartReason(request)} while waiting for a build slot.");
        }

        using var held = slot;
        if (!request.DeadlineIncludesQueue)
        {
            deadline.CancelAfter(request.Timeout);
        }

        if (deadline.IsCancellationRequested)
        {
            return new(-1, $"{request.Command} did not start: {StartReason(request)} before it could start.");
        }

        using var output = new ProcessOutput(request.LogPath);
        output.Note($"command: {request.Command}");
        output.Note($"directory: {request.Directory}");
        using var process = new Process { StartInfo = StartInfo(request) };
        var startFailure = Start(process, request, output);
        return startFailure is null
            ? await Supervise(process, request, output, deadline.Token)
            : new(-1, startFailure);
    }

    static async Task<IDisposable?> EnterSlot(ProcessRequest request, CancellationToken deadline)
    {
        try
        {
            return await GeneratedApplicationBuilds.Enter(new(request.Directory, request.FileName, request.Arguments), deadline);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    static ProcessStartInfo StartInfo(ProcessRequest request)
    {
        var startInfo = new ProcessStartInfo(request.FileName)
        {
            WorkingDirectory = request.Directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        // A generated application is a multi-project solution, so MSBuild starts worker nodes. Reused nodes outlive the build and keep the
        // redirected output pipes open, so reading the output would never finish. Build in fresh nodes that exit with the command.
        startInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        startInfo.Environment["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0";
        foreach (var argument in GeneratedApplicationBuilds.Capped(request.FileName, request.Arguments))
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    static string? Start(Process process, ProcessRequest request, ProcessOutput output)
    {
        try
        {
            process.Start();
            return null;
        }
        catch (System.ComponentModel.Win32Exception exception)
        {
            var message = $"{request.FileName} could not start: {exception.Message}";
            output.Note(message);
            return message;
        }
    }

    static async Task<ProcessResult> Supervise(Process process, ProcessRequest request, ProcessOutput output, CancellationToken deadline)
    {
        output.Note($"pid: {process.Id}");
        using var pumps = new CancellationTokenSource();
        var standardOutput = Pump(process.StandardOutput, text => output.Append(false, text), pumps.Token);
        var standardError = Pump(process.StandardError, text => output.Append(true, text), pumps.Token);
        var failure = await WaitForExit(process, request, output, deadline);
        var drained = await Drain(process, standardOutput, standardError, pumps);
        failure = WithDrainFailure(failure, drained, request.Command);
        if (failure is not null)
        {
            output.Note(failure);
            return new(-1, $"{failure}{Environment.NewLine}{output.Combined()}");
        }

        return new(process.ExitCode, output.Combined());
    }

    static async Task<string?> WaitForExit(Process process, ProcessRequest request, ProcessOutput output, CancellationToken deadline)
    {
        var processId = process.Id;
        try
        {
            await process.WaitForExitAsync(deadline);
            return null;
        }
        catch (OperationCanceledException)
        {
            return Reason(request) + await Terminate(process, processId, output);
        }
    }

    static async Task<bool> Drain(Process process, Task standardOutput, Task standardError, CancellationTokenSource pumps)
    {
        var all = Task.WhenAll(standardOutput, standardError);
        var drained = await Task.WhenAny(all, Task.Delay(_drainBudget)) == all;
        if (!drained)
        {
            // The output did not close in time: stop the pumps and release their pipes, then wait for them so none outlives the objects they use.
            await pumps.CancelAsync();
            process.StandardOutput.Dispose();
            process.StandardError.Dispose();
        }

        await standardOutput;
        await standardError;
        return drained;
    }

    static string? WithDrainFailure(string? failure, bool drained, string command)
    {
        if (drained)
        {
            return failure;
        }

        return failure is null
            ? $"{command} exited but its output did not close within {_drainBudget}."
            : $"{failure} Its output did not close within {_drainBudget}.";
    }

    static string StartReason(ProcessRequest request) =>
        request.CancellationToken.IsCancellationRequested ? "it was cancelled" : $"the {request.Timeout} deadline passed";

    static string Reason(ProcessRequest request) =>
        request.CancellationToken.IsCancellationRequested ? $"{request.Command} was cancelled." : $"{request.Command} did not finish within {request.Timeout}.";

    static async Task<string> Terminate(Process process, int processId, ProcessOutput output)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // The process exited between the deadline and the kill.
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or AggregateException)
        {
            var message = $" Killing process {processId} failed, so it may still be running: {exception.Message}";
            output.Note(message);
            return message;
        }

        using var budget = new CancellationTokenSource(_teardownBudget);
        try
        {
            await process.WaitForExitAsync(budget.Token);
            return string.Empty;
        }
        catch (OperationCanceledException)
        {
            var message = $" Process {processId} did not exit within {_teardownBudget} of being killed, so it may still be running.";
            output.Note(message);
            return message;
        }
    }

    static async Task Pump(StreamReader reader, Action<string> append, CancellationToken cancellationToken)
    {
        var buffer = new char[4096];
        try
        {
            int read;
            while ((read = await reader.ReadAsync(buffer.AsMemory(), cancellationToken)) > 0)
            {
                append(new string(buffer, 0, read));
            }
        }
        catch (Exception exception) when (cancellationToken.IsCancellationRequested && exception is OperationCanceledException or ObjectDisposedException or IOException)
        {
            // The pump was told to stop because the process's output did not close in time.
        }
    }

    /// <summary>
    /// The tool to run, where, for how long and, when logged, where its log goes.
    /// </summary>
    /// <param name="Directory">The working directory.</param>
    /// <param name="Timeout">How long the tool may run.</param>
    /// <param name="LogPath">The file the output is streamed to, or null when it is not logged.</param>
    /// <param name="FileName">The tool.</param>
    /// <param name="Arguments">The tool's arguments.</param>
    sealed record ToolRun(string Directory, TimeSpan Timeout, string? LogPath, string FileName, string[] Arguments);

    /// <summary>
    /// One tool run, whether the wait for a build slot counts against its deadline, and what cancels it.
    /// </summary>
    /// <param name="Tool">The tool to run.</param>
    /// <param name="DeadlineIncludesQueue">Whether the wait for a build slot counts against the deadline.</param>
    /// <param name="CancellationToken">Cancels the wait for a slot and the tool.</param>
    sealed record ProcessRequest(ToolRun Tool, bool DeadlineIncludesQueue, CancellationToken CancellationToken)
    {
        public string Directory => Tool.Directory;

        public TimeSpan Timeout => Tool.Timeout;

        public string? LogPath => Tool.LogPath;

        public string FileName => Tool.FileName;

        public string[] Arguments => Tool.Arguments;

        public string Command => $"{FileName} {string.Join(' ', Arguments)}";
    }

    /// <summary>
    /// Collects a process's output in memory and, when asked, streams it to a log as it arrives.
    /// </summary>
    sealed class ProcessOutput : IDisposable
    {
        readonly Lock _lock = new();
        readonly StringBuilder _standardOutput = new();
        readonly StringBuilder _standardError = new();
        StreamWriter? _log;

        public ProcessOutput(string? logPath)
        {
            if (logPath is null)
            {
                return;
            }

            var directory = Path.GetDirectoryName(Path.GetFullPath(logPath));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _log = new(new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.Read), new UTF8Encoding(false)) { AutoFlush = true };
        }

        public void Note(string line)
        {
            lock (_lock)
            {
                _log?.WriteLine($"# {line}");
            }
        }

        public void Append(bool error, string text)
        {
            lock (_lock)
            {
                (error ? _standardError : _standardOutput).Append(text);
                _log?.Write(text);
            }
        }

        public string Combined()
        {
            lock (_lock)
            {
                return $"{_standardOutput}{_standardError}";
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _log?.Dispose();
                _log = null;
            }
        }
    }
}

/// <summary>
/// The outcome of a tool run.
/// </summary>
/// <param name="ExitCode">The exit code; -1 when the tool could not start or timed out.</param>
/// <param name="Output">The combined standard output and error.</param>
public sealed record ProcessResult(int ExitCode, string Output);

/// <summary>
/// A tool run whose command, process id and output are streamed to a log while it runs.
/// </summary>
/// <param name="Directory">The working directory.</param>
/// <param name="Timeout">How long the wait for a build slot and the tool together may take.</param>
/// <param name="LogPath">The file the command, process id and output are written to while the tool runs.</param>
/// <param name="FileName">The tool.</param>
/// <param name="Arguments">The tool's arguments.</param>
public sealed record LoggedTool(string Directory, TimeSpan Timeout, string LogPath, string FileName, string[] Arguments);
