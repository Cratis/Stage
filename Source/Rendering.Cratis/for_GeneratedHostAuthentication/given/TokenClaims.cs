// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication.given;

sealed record TokenClaims
{
    internal string? Audience { get; init; }
    internal string? Issuer { get; init; }
    internal string? Tenant { get; init; }
    internal string? Nonce { get; init; }
    internal bool Expired { get; init; }
    internal bool Scope { get; init; } = true;
    internal bool AppRole { get; init; }
}
