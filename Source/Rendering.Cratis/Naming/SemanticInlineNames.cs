// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Naming;

/// <summary>
/// Allocates readable inline declarations together, before render-scope selection. Only conflicting
/// candidates gain a stable identity suffix; final spellings are checked again against every scope.
/// </summary>
internal sealed partial class SemanticInlineNames
{
    readonly Dictionary<string, string> _names;

    internal SemanticInlineNames(IEnumerable<SemanticInlineNameSite> sites, IEnumerable<SemanticInlineNameReservation> reservations)
    {
        var declarations = sites.ToArray();
        var reserved = reservations.ToLookup(reservation => reservation.Scope, reservation => reservation.Name, StringComparer.Ordinal);
        var stems = declarations.ToDictionary(site => site.Key, site => Identifiers.ToPascalCase(site.Stem), StringComparer.Ordinal);
        var names = new Dictionary<string, string>(stems, StringComparer.Ordinal);
        var lengths = declarations.ToDictionary(site => site.Key, _ => 0, StringComparer.Ordinal);
        var digests = declarations.ToDictionary(site => site.Key, site => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{site.Identity}:{site.Key}"))), StringComparer.Ordinal);
        while (Conflicts(declarations, names, reserved) is { Count: > 0 } conflicts)
        {
            foreach (var key in conflicts)
            {
                var length = lengths[key] == 0 ? 8 : lengths[key] + 4;
                if (length > digests[key].Length)
                {
                    throw new InvalidArtifactRenderContract($"Unable to allocate a unique inline declaration for '{key}'.");
                }
                lengths[key] = length;
                names[key] = $"{stems[key]}_{digests[key][..length]}";
            }
        }
        _names = names;
    }

    internal string this[string key] => _names[key];

    internal string HandlerBody(SemanticCommand command) => _names[Key("handler-body", command.Id)];
    internal string HandlerContext(SemanticCommand command) => _names[Key("handler-context", command.Id)];
    internal string HandlerCommand(SemanticCommand command) => _names[Key("handler-command", command.Id)];
    internal string GenerationBody(SemanticConcept concept) => _names[Key("generation-body", concept.Id)];

    static string Key(string role, SemanticId owner) => $"{role}:{owner}";

    static HashSet<string> Conflicts(SemanticInlineNameSite[] sites, Dictionary<string, string> names, ILookup<string, string> reservations)
    {
        var conflicts = new HashSet<string>(StringComparer.Ordinal);
        var scopes = sites.SelectMany(site => Scopes(site).Select(scope => (Site: site, scope.Scope, scope.Path)));
        foreach (var scope in scopes.GroupBy(item => (item.Scope, item.Path)))
        {
            var comparer = scope.Key.Path ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            var occupied = reservations[scope.Key.Scope].ToHashSet(comparer);
            foreach (var group in scope.GroupBy(item => names[item.Site.Key], comparer))
            {
                if (occupied.Contains(group.Key) || group.Skip(1).Any())
                {
                    conflicts.UnionWith(group.Select(item => item.Site.Key));
                }
            }
        }
        return conflicts;
    }

    static IEnumerable<(string Scope, bool Path)> Scopes(SemanticInlineNameSite site)
    {
        yield return (site.Scope, false);
        if (site.TypeScope is { } types) yield return (types, false);
        if (site.PathScope is { } paths) yield return (paths, true);
    }
}
