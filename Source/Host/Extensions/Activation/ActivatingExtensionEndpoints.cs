// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Activation;

/// <summary>
/// Finds backends by starting them when they are not running, for the systems whose extension came from a package, and leaves every other system to the backends the deployment configured to run by themselves.
/// </summary>
/// <param name="configured">The <see cref="IExtensionEndpoints"/> of the backends that run by themselves.</param>
/// <param name="activator">The <see cref="ExtensionActivator"/>.</param>
public sealed class ActivatingExtensionEndpoints(IExtensionEndpoints configured, ExtensionActivator activator) : IExtensionEndpoints
{
    /// <inheritdoc/>
    public async Task<ExtensionEndpoint?> Resolve(string system, CancellationToken cancellationToken)
    {
        if (!activator.HasBackend(system)) return await configured.Resolve(system, cancellationToken);

        return (await activator.Activate(system, ActivationReason.Invocation, cancellationToken)).Endpoint;
    }

    /// <inheritdoc/>
    public Task<bool> Knows(string system, CancellationToken cancellationToken) =>
        activator.HasBackend(system) ? Task.FromResult(true) : configured.Knows(system, cancellationToken);
}
