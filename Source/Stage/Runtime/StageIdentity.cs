// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Cratis.Stage.Runtime;

/// <summary>
/// Resolves the identity behind the current command from the authenticated caller on the HTTP request.
/// </summary>
/// <param name="httpContextAccessor">The accessor for the current HTTP context.</param>
public sealed class StageIdentity(IHttpContextAccessor httpContextAccessor) : IProvideStageIdentity
{
    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> Current()
    {
        if (httpContextAccessor.HttpContext?.User is not { Identity.IsAuthenticated: true } user)
        {
            return new Dictionary<string, string>();
        }

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Add(values, "id", user.FindFirstValue(ClaimTypes.NameIdentifier));
        Add(values, "name", user.FindFirstValue(ClaimTypes.Name) ?? user.Identity.Name);
        Add(values, "userName", user.Identity.Name);
        Add(values, "subject", user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier));

        return values;
    }

    static void Add(Dictionary<string, string> values, string key, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            values[key] = value;
        }
    }
}
