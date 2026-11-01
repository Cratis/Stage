// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>Ordinal, lexical lookup of guard data shapes and query result declarations.</summary>
internal sealed class GuardDeclarations
{
    readonly List<Declaration> _models = [];
    readonly List<Declaration> _queries = [];
    readonly ILookup<string, TypeSyntax> _types;
    readonly ILookup<string, ConceptSyntax> _concepts;

    internal GuardDeclarations(ApplicationSyntax syntax)
    {
        _types = (syntax.Types ?? []).ToLookup(type => type.Name, StringComparer.Ordinal);
        _concepts = syntax.Concepts.ToLookup(concept => concept.Name, StringComparer.Ordinal);
        foreach (var module in syntax.Modules) Collect(module.Features, [module.Name]);
    }

    internal (PropertySyntax Property, IReadOnlyList<string> Scope)? Property(string name, IReadOnlyList<string> scope, string field)
    {
        var shape = Shape(name, scope, new HashSet<string>(StringComparer.Ordinal));
        var property = Single((shape?.Properties ?? []).Where(property => property.Name == field));
        return property is null ? null : (property, shape!.Scope);
    }

    internal GuardFieldType Field(TypeRefSyntax subject, IReadOnlyList<string> scope, string path)
    {
        if (!path.StartsWith(SceneGuards.SubjectPrefix, StringComparison.Ordinal)) return GuardFieldType.Unknown("Not an item field");
        var segments = path[SceneGuards.SubjectPrefix.Length..].Split('.');
        var type = subject with { IsCollection = false };
        var optional = false;
        for (var index = 0; index < segments.Length; index++)
        {
            var resolved = Property(type.Name, scope, segments[index]);
            if (resolved is null) return GuardFieldType.Unknown($"Unknown or ambiguous field '{path}'");
            var (property, declaring) = resolved.Value;
            scope = declaring;
            type = property.Type;
            optional |= type.IsOptional;
            if (type.IsCollection && index < segments.Length - 1) return GuardFieldType.Unknown("Cannot traverse a collection");
        }

        return Classify(type, scope, new HashSet<string>(StringComparer.Ordinal)) with { IsOptional = optional };
    }

    GuardFieldType Classify(TypeRefSyntax type, IReadOnlyList<string> scope, HashSet<string> seen)
    {
        if (!seen.Add(type.Name)) return GuardFieldType.Unknown("Cyclic concept");
        var kind = type.Name switch
        {
            "String" => GuardFieldKind.Text,
            "Int" or "Integer" or "Long" or "Float" or "Double" or "Decimal" or "Number" => GuardFieldKind.Number,
            "Bool" or "Boolean" => GuardFieldKind.Boolean,
            "DateTime" or "DateTimeOffset" or "DateOnly" or "Date" => GuardFieldKind.DateTime,
            "Guid" => GuardFieldKind.Identifier,
            _ => GuardFieldKind.Unknown
        };
        GuardFieldType result = new(kind);
        if (kind == GuardFieldKind.Unknown) result = ClassifyDeclaration(type, scope, seen);
        return type.IsCollection && result.Kind != GuardFieldKind.Unknown ? result with { Kind = GuardFieldKind.Collection } : result;
    }

    GuardFieldType ClassifyDeclaration(TypeRefSyntax type, IReadOnlyList<string> scope, HashSet<string> seen)
    {
        var concepts = _concepts[type.Name].ToArray();
        if (concepts.Length == 1)
        {
            var concept = concepts[0];
            return concept.IsEnum
                ? new(GuardFieldKind.Enumeration, Members: [.. concept.Values], MemberNumbers: concept.ValueNumbers is null ? null : [.. concept.ValueNumbers])
                : Classify(type with { Name = concept.Type }, scope, seen);
        }

        return concepts.Length == 0 && Shape(type.Name, scope, new HashSet<string>(StringComparer.Ordinal)) is not null
            ? new(GuardFieldKind.Composite)
            : GuardFieldType.Unknown($"Unknown or ambiguous type '{type.Name}'");
    }

    Declaration? Shape(string name, IReadOnlyList<string> scope, HashSet<string> seen)
    {
        if (!seen.Add(name)) return null;
        var models = _models.Where(model => model.Name == name.Split('.')[^1]).ToArray();
        if (models.Length > 0) return Resolve(models, name, scope);
        var types = _types[name].ToArray();
        if (types.Length > 0) return types.Length == 1 ? new(name, [], types[0].Properties) : null;
        var query = Resolve(_queries, name, scope);
        return query?.Result is { } result ? Shape(result.Name, query.Scope, seen) : null;
    }

    Declaration? Resolve(IEnumerable<Declaration> declarations, string reference, IReadOnlyList<string> scope)
    {
        var segments = reference.Split('.');
        var candidates = declarations.Where(declaration => declaration.Name == segments[^1]).ToArray();
        if (segments.Length > 1)
        {
            var qualifiers = segments[..^1];
            return Single(candidates.Where(candidate => qualifiers.Length <= candidate.Scope.Count && candidate.Scope.TakeLast(qualifiers.Length).SequenceEqual(qualifiers, StringComparer.Ordinal)));
        }

        for (var depth = scope.Count; depth >= 0; depth--)
        {
            var visible = candidates.Where(candidate => depth <= candidate.Scope.Count && candidate.Scope.Take(depth).SequenceEqual(scope.Take(depth), StringComparer.Ordinal)).ToArray();
            if (visible.Length > 0) return Single(visible);
        }

        return null;
    }

    void Collect(IEnumerable<FeatureSyntax> features, IReadOnlyList<string> outer)
    {
        foreach (var feature in features)
        {
            string[] scope = [.. outer, feature.Name];
            foreach (var slice in feature.Slices)
            {
                string[] declaring = [.. scope, slice.Name];
                foreach (var model in slice.ReadModels ?? []) _models.Add(new(model.Name, declaring, model.Properties));
                foreach (var query in slice.Queries ?? []) _queries.Add(new(query.Name, declaring, [], query.ReturnType));
            }

            Collect(feature.Features, scope);
        }
    }

    T? Single<T>(IEnumerable<T> candidates)
        where T : class
    {
        var matches = candidates.Take(2).ToArray();
        return matches.Length == 1 ? matches[0] : null;
    }

    sealed record Declaration(string Name, IReadOnlyList<string> Scope, IEnumerable<PropertySyntax> Properties, TypeRefSyntax? Result = null);
}
