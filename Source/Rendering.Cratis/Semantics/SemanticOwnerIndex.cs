// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Locates declaration owners, including nested and future semantic node kinds.
/// Semantic identities used as references are not declaration owners.
/// </summary>
internal sealed class SemanticOwnerIndex
{
    readonly Dictionary<SemanticId, SemanticId> _emissions = [];
    readonly Dictionary<SemanticId, HashSet<SemanticId>> _contained = [];

    internal SemanticOwnerIndex(SemanticApplication application) => Visit(application, application.Id, false, []);

    internal bool Contains(SemanticId owner) => _emissions.ContainsKey(owner);

    internal IEnumerable<SemanticId> Omissions(SemanticId owner) => _contained[_emissions[owner]];

    static bool IsSemanticRecord(Type type) => type.Assembly == typeof(SemanticApplication).Assembly && !type.IsEnum && !type.IsPrimitive;

    static bool StartsEmission(object node, bool declaration) => !declaration || node is
        SemanticSlice or SemanticModule or SemanticFeature;

    static bool IsDefaultArray(object node, Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ImmutableArray<>) &&
        type.GetProperty(nameof(ImmutableArray<>.IsDefault))!.GetValue(node) is true;

    void Visit(object? node, SemanticId emission, bool declaration, ImmutableArray<SemanticId> containers)
    {
        if (node is null or string or SemanticId) return;
        var type = node.GetType();
        if (IsDefaultArray(node, type)) return;
        if (node is IEnumerable sequence)
        {
            foreach (var child in sequence) Visit(child, emission, declaration, containers);
            return;
        }
        if (!IsSemanticRecord(type)) return;
        VisitDeclaration(node, emission, declaration, containers);
    }

    void VisitDeclaration(object node, SemanticId emission, bool declaration, ImmutableArray<SemanticId> containers)
    {
        var type = node.GetType();
        if (type.GetProperty("Id")?.GetValue(node) is SemanticId { IsSet: true } id)
        {
            if (StartsEmission(node, declaration)) emission = id;
            declaration = node is not (SemanticApplication or SemanticModule or SemanticFeature);
            Remember(id, emission, containers);
            if (node is SemanticModule or SemanticFeature or SemanticSlice) containers = containers.Add(id);
        }

        // Walk semantic records, not identity-reference values. No list of declaration kinds can
        // stay exhaustive as the compiler grows; every declared Id participates automatically.
        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(property => property.GetIndexParameters().Length == 0))
        {
            Visit(property.GetValue(node), emission, declaration, containers);
        }
    }

    void Remember(SemanticId id, SemanticId emission, ImmutableArray<SemanticId> containers)
    {
        _emissions[id] = emission;
        Add(emission, id);
        foreach (var ancestor in containers) Add(ancestor, id);
    }

    void Add(SemanticId emission, SemanticId owner)
    {
        if (!_contained.TryGetValue(emission, out var owners)) _contained[emission] = owners = [];
        owners.Add(owner);
    }
}
