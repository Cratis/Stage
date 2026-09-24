// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Stage.Runtime;

/// <summary>
/// Represents an <see cref="ITenantIdAccessor"/> that always answers the default tenant.
/// </summary>
/// <remarks>
/// Used by the constructors that predate tenant resolution, so existing callers keep the tenant they had before:
/// the sandbox's default tenant.
/// </remarks>
internal sealed class DefaultTenantIdAccessor : ITenantIdAccessor
{
    /// <summary>
    /// Gets the shared instance.
    /// </summary>
    public static DefaultTenantIdAccessor Instance { get; } = new();

    /// <inheritdoc/>
    public TenantId Current => TenantId.Default;
}
