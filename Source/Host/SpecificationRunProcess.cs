// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;

namespace Cratis.Stage.Host;

internal interface ISpecificationRunProcess
{
    Task<SpecificationProcessResult> Run(ProcessStartInfo start, CancellationToken cancellationToken);
}

internal sealed record SpecificationProcessResult(int ExitCode, string Output, string Error);

internal sealed class SpecificationRunProcess : ISpecificationRunProcess
{
    public async Task<SpecificationProcessResult> Run(ProcessStartInfo start, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var process = new Process { StartInfo = start };
        process.Start();
        var output = Drain(process.StandardOutput, cancellationToken);
        var error = Drain(process.StandardError, cancellationToken);
        try
        {
            await process.WaitForExitAsync(cancellationToken);
            return new(process.ExitCode, await output, await error);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await process.WaitForExitAsync(cleanup.Token);
            }

            // Observe both readers even when cancellation interrupts the process.
            await ((Task)Task.WhenAll(output, error)).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }

    static async Task<string> Drain(StreamReader reader, CancellationToken cancellationToken)
    {
        const int limit = 8192;
        var result = new StringBuilder();
        var buffer = new char[1024];
        int count;
        while ((count = await reader.ReadAsync(buffer.AsMemory(), cancellationToken)) > 0)
        {
            result.Append(buffer, 0, Math.Min(count, limit - result.Length));
        }
        return result.ToString();
    }
}
