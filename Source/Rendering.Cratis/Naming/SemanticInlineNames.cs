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
        var qualified = declarations.Where(site => site.QualifiedStem is not null)
            .ToDictionary(site => site.Key, site => Identifiers.ToPascalCase(site.QualifiedStem!), StringComparer.Ordinal);
        _names = Allocate(declarations, reserved, stems, qualified);
    }

    internal string this[string key] => _names[key];

    internal string HandlerBody(SemanticCommand command) => _names[Key("handler-body", command.Id)];
    internal string HandlerContext(SemanticCommand command) => _names[Key("handler-context", command.Id)];
    internal string HandlerCommand(SemanticCommand command) => _names[Key("handler-command", command.Id)];
    internal string GenerationBody(SemanticConcept concept) => _names[Key("generation-body", concept.Id)];

    internal string ReducerTransition(string requirementId) => _names[TransitionKey(requirementId)];

    static string TransitionKey(string requirementId) => $"reducer-transition:{requirementId}";

    static string Key(string role, SemanticId owner) => $"{role}:{owner}";

    static Dictionary<string, string> Allocate(
        SemanticInlineNameSite[] declarations,
        ILookup<string, string> reserved,
        Dictionary<string, string> stems,
        Dictionary<string, string> qualified)
    {
        var bases = new Dictionary<string, string>(stems, StringComparer.Ordinal);
        var names = new Dictionary<string, string>(stems, StringComparer.Ordinal);
        var lengths = declarations.ToDictionary(site => site.Key, _ => 0, StringComparer.Ordinal);
        var digests = declarations.ToDictionary(site => site.Key, site => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{site.Identity}:{site.Key}"))), StringComparer.Ordinal);
        while (Conflicts(declarations, names, reserved) is { Count: > 0 } conflicts)
        {
            foreach (var key in conflicts)
            {
                if (lengths[key] == 0 && Qualification(key, qualified, bases) is { } owned)
                {
                    names[key] = owned;
                    bases[key] = owned;
                    continue;
                }

                var length = lengths[key] == 0 ? 8 : lengths[key] + 4;
                if (length > digests[key].Length)
                {
                    throw new InvalidArtifactRenderContract($"Unable to allocate a unique inline declaration for '{key}'.");
                }
                lengths[key] = length;
                names[key] = $"{bases[key]}_{digests[key][..length]}";
            }
        }

        return names;
    }

    static string? Qualification(string key, Dictionary<string, string> qualified, Dictionary<string, string> bases)
    {
        if (!qualified.TryGetValue(key, out var owned)) return null;
        return bases[key] == owned ? null : owned;
    }

    static HashSet<string> Conflicts(SemanticInlineNameSite[] sites, Dictionary<string, string> names, ILookup<string, string> reservations)
    {
        var conflicts = new HashSet<string>(StringComparer.Ordinal);
        var scopes = sites.SelectMany(site => Scopes(site).Select(scope => (Site: site, scope.Scope, scope.Path)));
        foreach (var scope in scopes.GroupBy(item => (item.Scope, item.Path)))
        {
            var comparer = scope.Key.Path ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            var occupied = reservations[scope.Key.Scope].ToHashSet(comparer);
            var spellings = scope.SelectMany(item => Spellings(item.Site, names[item.Site.Key]).Select(spelling => (item.Site.Key, Spelling: spelling)));
            foreach (var group in spellings.GroupBy(item => item.Spelling, comparer))
            {
                if (occupied.Contains(group.Key) || group.Select(item => item.Key).Distinct(StringComparer.Ordinal).Skip(1).Any())
                {
                    conflicts.UnionWith(group.Select(item => item.Key));
                }
            }
        }
        return conflicts;
    }

    static IEnumerable<string> Spellings(SemanticInlineNameSite site, string name) =>
        site.Derived is null ? [name] : new[] { name }.Concat(site.Derived(name));

    static IEnumerable<(string Scope, bool Path)> Scopes(SemanticInlineNameSite site)
    {
        yield return (site.Scope, false);
        if (site.TypeScope is { } types) yield return (types, false);
        if (site.PathScope is { } paths) yield return (paths, true);
    }
}
