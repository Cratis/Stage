// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Stage.Host.Extensions;

/// <summary>
/// Reads the backends from <see cref="ExtensionOptions.Backends"/>.
/// </summary>
/// <param name="options">The <see cref="ExtensionOptions"/>.</param>
public sealed class ConfiguredExtensionEndpoints(IOptions<ExtensionOptions> options) : IExtensionEndpoints
{
    /// <inheritdoc/>
    public Task<ExtensionEndpoint?> Resolve(string system, CancellationToken cancellationToken)
    {
        var found = options.Value.Backends.FirstOrDefault(pair => string.Equals(pair.Key, system, StringComparison.OrdinalIgnoreCase));
        if (found.Value is not { Endpoint.Length: > 0 } backend || !Uri.TryCreate(backend.Endpoint, UriKind.Absolute, out var address))
        {
            return Task.FromResult<ExtensionEndpoint?>(null);
        }

        return Task.FromResult<ExtensionEndpoint?>(new ExtensionEndpoint(system, address));
    }
}
