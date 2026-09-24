// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Api;
using Microsoft.AspNetCore.Http;

namespace Cratis.Stage.Semantics;

internal static class SemanticCommandAccess
{
    const string OccurrenceKey = "Stage.Semantic.Occurrence";

    internal static SemanticCommandOccurrence Occurrence(CommandContext context, ClaimsPrincipal principal)
    {
        if (context.Values.TryGetValue(OccurrenceKey, out var stored) && stored is SemanticCommandOccurrence occurrence)
        {
            return occurrence;
        }

        occurrence = new(
            DateTimeOffset.UtcNow,
            principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown",
            principal.FindFirstValue(ClaimTypes.Name) ?? "unknown",
            principal.Identity?.Name ?? "unknown");
        context.Values[OccurrenceKey] = occurrence;
        return occurrence;
    }

    internal static ClaimsPrincipal Principal(IHttpContextAccessor accessor) => accessor.HttpContext?.User ?? new ClaimsPrincipal();

    internal static IReadOnlyDictionary<string, System.Text.Json.JsonElement> Payload(CommandContext context) =>
        context.Command is DynamicCommand command ? command.Data.AsReadOnly() : new Dictionary<string, System.Text.Json.JsonElement>();
}
