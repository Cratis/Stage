// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_guarding_the_authentication_perimeter.given;

public sealed record request_headers
{
    public bool Cookie { get; init; }
    public string? Authorization { get; init; }
    public string? Origin { get; init; }
    public string? Mode { get; init; }
    public string? Accept { get; init; }

    public void Apply(HttpRequestMessage request)
    {
        if (Cookie) request.Headers.Add("Cookie", "perimeter-session=synthetic-session");
        if (Authorization is not null) request.Headers.TryAddWithoutValidation("Authorization", Authorization);
        if (Origin is not null) request.Headers.Add("Origin", Origin);
        if (Mode is not null) request.Headers.Add("Sec-Fetch-Mode", Mode);
        if (Accept is not null) request.Headers.Add("Accept", Accept);
    }
}
