// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Stage.Host.Extensions;

/// <summary>
/// The identity of a backend that calls the host services. It is a type of its own, not a claim, so nothing a token carries can forge it.
/// </summary>
public sealed class ExtensionIdentity : ClaimsIdentity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExtensionIdentity"/> class.
    /// </summary>
    /// <param name="system">The name of the system the backend serves.</param>
    public ExtensionIdentity(string system)
        : base("Cratis.Stage.Extension", ClaimTypes.Name, ClaimTypes.Role)
    {
        System = system;
        AddClaim(new Claim(ClaimTypes.Name, $"extension:{system}"));
    }

    /// <summary>
    /// Gets the name of the system the backend serves.
    /// </summary>
    public string System { get; }
}
