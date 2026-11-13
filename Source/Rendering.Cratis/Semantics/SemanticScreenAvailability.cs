// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Publishes screens as exact-name parts without assigning them a command/query slice location.
/// </summary>
internal static class SemanticScreenAvailability
{
    internal static IReadOnlyList<Part> Parts(SemanticApplicationContext context)
    {
        var keys = Map(context);
        return [.. context.AllSlices.SelectMany(located => located.Slice.Screens
                .Where(screen => keys.ContainsKey(screen.Name))
                .Select(screen => Create(located, screen, keys[screen.Name])))
            .OrderBy(part => part.Key, StringComparer.Ordinal)];
    }

    internal static IReadOnlyDictionary<string, string> Map(SemanticApplicationContext context) => ScreenAvailabilityParts.For(context.Application);

    internal static void AppendTargets(Dictionary<string, string> paths, SemanticApplicationContext context)
    {
        foreach (var screen in Parts(context)) paths[screen.Key] = screen.Target;
    }

    internal static void AppendScopes(CSharpCodeBuilder builder, SemanticApplicationContext context, string engine)
    {
        foreach (var screen in Parts(context))
        {
            builder.Line($"    [{CSharpCodeBuilder.StringLiteral(screen.Key)}] = {engine}.Context.ContextScope.Of({string.Join(", ", screen.Scope.Select(CSharpCodeBuilder.StringLiteral))}),");
        }
    }

    internal static string ScopeResolver(SemanticApplicationContext context, string engine) => Parts(context).Count == 0
        ? $"    options.ScopeFor = type => Scopes.TryGetValue(type.Namespace ?? string.Empty, out var scope) ? scope : {engine}.Context.ContextScope.Application;"
        : $"    options.ScopeFor = type => Scopes.TryGetValue(type.FullName ?? string.Empty, out var screenScope) ? screenScope : Scopes.TryGetValue(type.Namespace ?? string.Empty, out var scope) ? scope : {engine}.Context.ContextScope.Application;";

    internal static string Render(SemanticApplicationContext context)
    {
        // No namespace: SliceLocation.For must be empty, even with custom namespace roots or segment skipping.
        // Otherwise closing a screen could hide command/query proxies in that slice on an Arc client.
        var builder = new CSharpCodeBuilder();
        foreach (var part in Parts(context))
        {
            builder.Summary($"The availability part for screen '{part.Target}'.")
                .Line($"public sealed class {part.Key};").BlankLine();
        }

        return builder.ToString();
    }

    static string[] ScopeOf(LocatedSemanticSlice located) => [located.Module.Name, .. located.FeaturePath.Select(feature => feature.Name), located.Slice.Name];

    static Part Create(LocatedSemanticSlice located, SemanticScreen screen, string key) => new(
        screen.Name,
        key,
        $"screen:{string.Join('/', ScopeOf(located).Append(screen.Name))}",
        ScopeOf(located),
        [.. located.Module.Available, .. located.FeaturePath.SelectMany(feature => feature.Available), .. located.Slice.Available, .. screen.Available]);

    internal sealed record Part(string Name, string Key, string Target, string[] Scope, ImmutableArray<SemanticAvailabilityRequirement> Requirements);
}
