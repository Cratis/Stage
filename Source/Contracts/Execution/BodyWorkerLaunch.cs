// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;

namespace Cratis.Stage.Contracts.Execution;

/// <summary>The worker executable or a CLI executable with its hidden worker verb.</summary>
/// <param name="FileName">The executable to start.</param>
/// <param name="Arguments">The arguments passed to the executable.</param>
public sealed record BodyWorkerLaunch(string FileName, ImmutableArray<string> Arguments)
{
    /// <summary>
    /// Relaunches the current executable with a hidden worker verb, including the entry assembly for a dotnet host.
    /// </summary>
    /// <param name="verb">The host's hidden worker verb.</param>
    /// <returns>The executable and its worker arguments.</returns>
    /// <exception cref="InvalidOperationException">The current process path is unavailable.</exception>
    public static BodyWorkerLaunch ForCurrentProcess(string verb) => ForCurrentProcess(verb, Environment.ProcessPath, Assembly.GetEntryAssembly()?.Location ?? string.Empty);

    internal static BodyWorkerLaunch ForCurrentProcess(string verb, string? processPath, string entryAssemblyPath)
    {
        if (processPath is null) throw new InvalidOperationException("The current process path is unavailable.");
        var name = Path.GetFileName(processPath);
        return new(processPath, name.Equals("dotnet", StringComparison.OrdinalIgnoreCase) || name.Equals("dotnet.exe", StringComparison.OrdinalIgnoreCase)
            ? [entryAssemblyPath, verb] : [verb]);
    }
}
