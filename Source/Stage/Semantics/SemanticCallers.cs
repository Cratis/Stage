// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Semantics;

internal static class SemanticCallers
{
    internal static SemanticCaller From(ClaimsPrincipal principal) => new(
        principal.Identity?.IsAuthenticated == true,
        [.. principal.FindAll(ClaimTypes.Role).Select(claim => claim.Value)],
        [.. principal.Claims.Where(claim => claim.Type != ClaimTypes.Role).Select(claim => new SemanticCallerClaim(claim.Type, claim.Value))]);
}
