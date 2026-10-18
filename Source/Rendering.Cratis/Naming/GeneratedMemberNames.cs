// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Naming;

/// <summary>
/// Resolves generated member names once by semantic identity, without changing authored contracts.
/// </summary>
internal sealed class GeneratedMemberNames
{
    readonly Dictionary<SemanticId, string> _properties = [];
    readonly Dictionary<SemanticId, string> _queries = [];

    internal GeneratedMemberNames(IEnumerable<SemanticCommand> commands, IEnumerable<SemanticReadModel> readModels, IEnumerable<SemanticKeyedQuery> queries)
    {
        foreach (var command in commands)
        {
            foreach (var property in command.Properties.Where(property => !property.IsGenerated))
            {
                _properties.Add(property.Id, GeneratedPascalCase.CommandProperty(command.Name, property.Name));
            }
        }
        var models = readModels.ToDictionary(model => model.Id);
        foreach (var query in queries)
        {
            var model = models[query.ReadModel];
            _queries.Add(query.Id, GeneratedPascalCase.QueryMethod(model.Name, model.Properties.Select(property => property.Name), query.Name));
        }
    }

    internal string Property(SemanticProperty property) => _properties.GetValueOrDefault(property.Id) ?? Identifiers.ToPascalCase(property.Name);

    internal string Query(SemanticKeyedQuery query) => _queries[query.Id];

    internal string PropertyPath(IEnumerable<SemanticProperty> properties, string path)
    {
        var segments = path.Split('.');
        var property = properties.Single(property => string.Equals(property.Name, segments[0], StringComparison.OrdinalIgnoreCase));
        return string.Join('.', new[] { Property(property) }.Concat(segments.Skip(1).Select(Identifiers.ToPascalCase)));
    }

    internal string WireProperty(SemanticProperty property) => Property(property) == Identifiers.ToPascalCase(property.Name)
        ? property.Name : ProxyProperty(property);

    internal string ProxyProperty(SemanticProperty property)
    {
        var member = Property(property);

        // Arc preserves leading acronyms when camel-casing CLR property names.
        return member.Length >= 2 && char.IsUpper(member[0]) && char.IsUpper(member[1])
            ? member
            : char.ToLowerInvariant(member[0]) + member[1..];
    }
}
