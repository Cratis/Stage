// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Names fixture expressions within one specification, retaining single-use simple literals inline.
/// </summary>
/// <param name="context">The indexed semantic application.</param>
/// <param name="specification">The specification whose values are named.</param>
internal sealed class SemanticSpecificationValuesRenderer(SemanticApplicationContext context, SemanticSpecification specification)
{
    internal const string Declarations = "/* specification fixture declarations */";
    readonly Dictionary<(string Type, string Expression), Fixture> _fixtures = [];
    readonly HashSet<string> _names = new(StringComparer.Ordinal)
    {
        "_scenario", "_reactor", "_result", "_results", "_givenEventCount", "_facts", "_settled", "_principal", "_principalAccessor", "_readModels", "_readModelScenario", "_store", "_log", "_outbox", "_inbox", "_history", "_published", "_produced"
    };

    internal string Value(SemanticValue value, SemanticTypeReference type, string expression, SemanticTypeSystem types)
    {
        if (value is SemanticNullValue) return expression;
        var key = (types.Type(type), expression);
        if (!_fixtures.TryGetValue(key, out var fixture))
        {
            var stem = $"_{Identifiers.ToCamelCase(PropertyName(value, type))}";
            var name = stem;
            for (var suffix = 2; !_names.Add(name); suffix++) name = $"{stem}{suffix}";
            var complex = type.IsCollection || type.Kind == SemanticTypeReferenceKind.CompositeType ||
                (type.Kind == SemanticTypeReferenceKind.Concept ? context.Concepts[type.Target].Primitive : type.Primitive)
                    is SemanticPrimitiveType.Uuid or SemanticPrimitiveType.Date or SemanticPrimitiveType.DateTime;
            fixture = new(key.Item1, expression, name, $"\u001Ffixture{_fixtures.Count}\u001F", complex);
            _fixtures.Add(key, fixture);
        }

        return fixture.Token;
    }

    internal string Render(string content)
    {
        var declarations = new List<string>();
        foreach (var fixture in _fixtures.Values)
        {
            // Count emitted references, not renderer calls: some renderers compute unused alternatives.
            var count = content.Split(fixture.Token, StringSplitOptions.None).Length - 1;
            var hoisted = count > 0 && (fixture.Complex || count > 1);
            if (hoisted) declarations.Add($"    static readonly {fixture.Type} {fixture.Name} = {fixture.Expression};");
            content = content.Replace(fixture.Token, hoisted ? fixture.Name : fixture.Expression, StringComparison.Ordinal);
        }

        return content.Replace(
            $"    {Declarations}{Environment.NewLine}",
            declarations.Count == 0 ? string.Empty : string.Join(Environment.NewLine, declarations) + Environment.NewLine,
            StringComparison.Ordinal);
    }

    string PropertyName(SemanticValue value, SemanticTypeReference type)
    {
        var properties = context.Commands.Values.SelectMany(command => command.Properties)
            .Concat(context.Events.Values.SelectMany(contract => contract.Properties))
            .Concat(context.ReadModels.Values.SelectMany(model => model.Properties)).ToArray();
        var bindings = (specification.When?.Values ?? [])
            .Concat(specification.WhenAppended?.Values ?? [])
            .Concat(specification.GivenEvents.SelectMany(given => given.Values))
            .Concat(specification.ThenEvents.SelectMany(expected => expected.Values))
            .Concat(specification.ThenReadModels.SelectMany(expected => expected.Values))
            .Concat(specification.ThenQueries.SelectMany(query => query.Results).SelectMany(result => result.Values));
        foreach (var binding in bindings.Where(binding => Equals(binding.Value, value)))
        {
            if (properties.FirstOrDefault(property => property.Id == binding.TargetProperty && property.Type == type) is { } property)
                return property.Name;
        }

        return type.Kind switch
        {
            SemanticTypeReferenceKind.Concept => context.Concepts[type.Target].Name,
            SemanticTypeReferenceKind.CompositeType => context.Types[type.Target].Name,
            _ => type.Primitive.ToString()
        };
    }

    sealed record Fixture(string Type, string Expression, string Name, string Token, bool Complex);
}
