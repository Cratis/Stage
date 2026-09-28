// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Api;
using Cratis.Stage.Specifications.Commands;

namespace Cratis.Stage.Specifications.Types;

/// <summary>
/// Emits CLR event and read-model types for the scalar contracts admitted by the in-memory runner.
/// </summary>
/// <param name="plan">The semantic contract plan.</param>
internal sealed class SemanticRuntimeTypes(SemanticExecutionPlan plan)
{
    readonly DynamicTypeFactory _factory = new();
    readonly Dictionary<SemanticId, Type> _events = [];
    readonly Dictionary<SemanticId, Type> _commands = [];
    readonly Dictionary<SemanticId, Type> _readModels = [];

    internal static object? ConvertValue(SemanticValue value, Type target) => value switch
    {
        SemanticNullValue => null,
        SemanticTextValue text when target == typeof(Guid) => Guid.Parse(text.Value),
        SemanticTextValue text when target == typeof(DateOnly) => DateOnly.Parse(text.Value, CultureInfo.InvariantCulture),
        SemanticTextValue text when target == typeof(DateTimeOffset) => DateTimeOffset.Parse(text.Value, CultureInfo.InvariantCulture),
        SemanticTextValue text => text.Value,
        SemanticNumberValue number when target == typeof(long) => decimal.ToInt64(number.Value),
        SemanticNumberValue number => number.Value,
        SemanticBooleanValue boolean => boolean.Value,
        _ => throw new UnsupportedSemanticMapping()
    };

    internal Type ForCommand(SemanticCommand command)
    {
        if (!_commands.TryGetValue(command.Id, out var type))
        {
            type = _factory.CreateCommandType("Stage.Semantic.Run", $"Command_{_commands.Count}");
            _commands.Add(command.Id, type);
        }

        return type;
    }

    internal Type ForReadModel(SemanticReadModel readModel)
    {
        if (!_readModels.TryGetValue(readModel.Id, out var type))
        {
            type = _factory.CreateReadModelType("Stage.Semantic.ReadModels", $"ReadModel_{_readModels.Count}", readModel.Properties.ToDictionary(property => property.Name, property => Resolve(property.Type)));
            _readModels.Add(readModel.Id, type);
        }

        return type;
    }

    internal object Materialize(SemanticSpecificationEvent fact)
    {
        var contract = plan.Events[fact.EventContract];
        var type = For(contract.Id);
        var instance = Activator.CreateInstance(type)!;
        foreach (var value in fact.Values)
        {
            var property = contract.Properties.Single(property => property.Id == value.TargetProperty);
            type.GetProperty(property.Name)!.SetValue(instance, ConvertValue(value.Value, Resolve(property.Type)));
        }

        return instance;
    }

    internal Type For(SemanticId eventContract)
    {
        if (_events.TryGetValue(eventContract, out var existing))
        {
            return existing;
        }

        var contract = plan.Events[eventContract];
        var type = _factory.CreateEventType("Stage.Semantic.Events", $"Event_{_events.Count}", contract.ContractId.ToString(), contract.Properties.ToDictionary(property => property.Name, property => Resolve(property.Type)));
        _events.Add(eventContract, type);
        return type;
    }

    Type Resolve(SemanticTypeReference reference)
    {
        var primitive = reference.Kind == SemanticTypeReferenceKind.Concept
            ? plan.Model.Application.Concepts.Single(concept => concept.Id == reference.Target).Primitive
            : reference.Primitive;
        return primitive switch
        {
            SemanticPrimitiveType.Uuid => typeof(Guid),
            SemanticPrimitiveType.Text => typeof(string),
            SemanticPrimitiveType.WholeNumber => typeof(long),
            SemanticPrimitiveType.DecimalNumber => typeof(decimal),
            SemanticPrimitiveType.Boolean => typeof(bool),
            SemanticPrimitiveType.Date => typeof(DateOnly),
            SemanticPrimitiveType.DateTime => typeof(DateTimeOffset),
            _ => typeof(object)
        };
    }
}
