// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication.given;

sealed record HostRequest
{
    internal string Method { get; init; } = "GET";
    internal string? Cookie { get; init; }
    internal string? Authorization { get; init; }
    internal string? Origin { get; init; }
    internal bool Navigate { get; init; }
}
