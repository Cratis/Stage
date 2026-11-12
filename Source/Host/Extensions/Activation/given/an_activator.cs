// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Host.Extensions.given;
using Cratis.Stage.Host.Extensions.Packages;
using Cratis.Stage.Host.Extensions.Packages.given;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Stage.Host.Extensions.Activation.given;

/// <summary>
/// A specification with an activator over fake backends.
/// </summary>
public abstract class an_activator : a_backend_package
{
    protected ManualTime _time = null!;
    protected FakeBackendRuntime _runtime = null!;
    protected ExtensionOptions _options = null!;
    protected ExtensionActivator _activator = null!;
    protected List<string> _stopped = null!;

    /// <summary>
    /// Creates the activator for some systems that all name the same package.
    /// </summary>
    /// <param name="activation">The default the package's contract states, or empty.</param>
    /// <param name="overrides">The consumer's overrides by system.</param>
    /// <param name="systems">The systems of the model.</param>
    /// <returns>A task that completes when the activator is ready.</returns>
    protected async Task Prepare(string activation, Dictionary<string, ActivationOverride> overrides, params string[] systems)
    {
        _time = new();
        _runtime = new(ExecutionTier.SharedHost);
        _stopped = [];
        _options = new();
        _options.Execution.Sharing.Publishers = [PackageFixtures.Publisher];
        _options.Execution.Hosts["dotnet"] = new() { Command = "unused" };
        await LoadBackend("dotnet", activation);

        var packages = new ExtensionPackages(_result, systems.ToDictionary(system => system, _ => PackageFixtures.Reference));
        var systemsOfModel = new ActivatedSystems(packages, new ModelActivationOverrides(overrides), Options.Create(_options));
        _activator = new(systemsOfModel, [_runtime], Options.Create(_options), _time, NullLogger<ExtensionActivator>.Instance);
        _activator.Stopped += _stopped.Add;
    }

    protected void Idle(TimeSpan by) => _time.Advance(by);
}
