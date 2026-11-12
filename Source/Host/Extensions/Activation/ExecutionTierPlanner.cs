// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Stage.Host.Extensions.Packages;

namespace Cratis.Stage.Host.Extensions.Activation;

/// <summary>
/// How one system's backend will run.
/// </summary>
/// <param name="Tier">The <see cref="ExecutionTier"/>.</param>
/// <param name="Runtime">The runtime the contract names, or empty when there is no backend.</param>
/// <param name="Refusal">Why the backend cannot run on this deployment, or empty.</param>
public sealed record ExecutionPlan(ExecutionTier Tier, string Runtime, string Refusal = "")
{
    /// <summary>
    /// Gets whether the backend can be started.
    /// </summary>
    public bool CanRun => Tier != ExecutionTier.None && Refusal.Length == 0;
}

/// <summary>
/// Settles the tier of a backend. The contract says what the backend is; the deployment says what may share a host.
/// </summary>
/// <param name="options">The <see cref="ExecutionOptions"/>.</param>
public sealed class ExecutionTierPlanner(ExecutionOptions options)
{
    static readonly FrozenSet<string> _sharedRuntimes = new[] { "dotnet", "node", "jvm" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    static readonly FrozenSet<string> _nativeRuntimes = new[] { "native", "go" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets whether a pattern list holds a value.
    /// </summary>
    /// <param name="patterns">Exact values or prefixes ending in <c lang="text">*</c>.</param>
    /// <param name="value">The value.</param>
    /// <returns>True when a pattern matches.</returns>
    public static bool Matches(string[] patterns, string value) => patterns.Any(pattern => pattern.EndsWith('*')
        ? value.StartsWith(pattern[..^1], StringComparison.Ordinal)
        : string.Equals(pattern, value, StringComparison.Ordinal));

    /// <summary>
    /// Plans how the backend of a system runs.
    /// </summary>
    /// <param name="system">The name of the system.</param>
    /// <param name="package">The package whose backend it is.</param>
    /// <returns>The <see cref="ExecutionPlan"/>.</returns>
    public ExecutionPlan Plan(string system, LoadedExtensionPackage package)
    {
        if (package.Contract.Backend is not { } backend) return new(ExecutionTier.None, string.Empty);

        var runtime = backend.Runtime;
        if (_nativeRuntimes.Contains(runtime)) return new(ExecutionTier.Native, runtime);

        if (_sharedRuntimes.Contains(runtime) && MayShare(package))
        {
            return options.Hosts.TryGetValue(runtime, out var host) && host.Command.Length > 0
                ? new(ExecutionTier.SharedHost, runtime)
                : new(ExecutionTier.SharedHost, runtime, $"The deployment lets {package.Lock.Resolved} share a {runtime} host but configures none under Hosts:{runtime}.");
        }

        return options.Containers.TryGetValue(system, out var container) && container.Image.Length > 0
            ? new(ExecutionTier.Container, runtime)
            : new(ExecutionTier.Container, runtime, $"{package.Lock.Resolved} is not trusted to share a host, so {system} needs a container, and none is configured under Containers:{system}.");
    }

    bool MayShare(LoadedExtensionPackage package) =>
        Matches(options.Sharing.Publishers, package.Lock.Publisher) || Matches(options.Sharing.References, package.Lock.Resolved);
}
