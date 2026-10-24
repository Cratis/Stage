// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Api;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.Projections;
using Cratis.Stage.Specifications.Commands;
using Cratis.Stage.Specifications.Comparison;

namespace Cratis.Stage.Specifications.Types;

/// <summary>
/// Emits CLR event and read-model types for the contracts admitted by the in-memory runner.
/// </summary>
/// <param name="plan">The semantic contract plan.</param>
internal sealed class SemanticRuntimeTypes(SemanticExecutionPlan plan)
{
    static readonly Dictionary<SemanticPrimitiveType, Type> _clrTypes = new()
    {
        [SemanticPrimitiveType.Uuid] = typeof(Guid),
        [SemanticPrimitiveType.Text] = typeof(string),
        [SemanticPrimitiveType.WholeNumber] = typeof(long),
        [SemanticPrimitiveType.DecimalNumber] = typeof(decimal),
        [SemanticPrimitiveType.Boolean] = typeof(bool),
        [SemanticPrimitiveType.Date] = typeof(DateOnly),
        [SemanticPrimitiveType.DateTime] = typeof(DateTimeOffset)
    };

    readonly DynamicTypeFactory _factory = new();
    readonly Dictionary<SemanticId, Type> _events = [];
    readonly Dictionary<SemanticId, Type> _commands = [];
    readonly Dictionary<SemanticId, Type> _readModels = [];
    readonly Dictionary<SemanticId, Type> _composites = [];

    internal static object? ConvertValue(SemanticValue value, Type target) =>
        ConvertUnderlying(value, Nullable.GetUnderlyingType(target) ?? target);

    internal static object? ConvertUnderlying(SemanticValue value, Type target) => value switch
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

    internal static string ProductMember(SemanticProjectionProduct product) => char.ToLowerInvariant(product.Name[0]) + product.Name[1..];

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
            type = _factory.CreateReadModelType("Stage.Semantic.ReadModels", $"ReadModel_{_readModels.Count}", readModel.Properties.ToDictionary(property => property.Name, property => ResolveReadModel(property.Type)));
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
            var converted = ReadModelValue(value.Value, property.Type);
            type.GetProperty(property.Name)!.SetValue(instance, converted);
        }

        // Chronicle's Add and Subtract read one stored event property, so a product the projections add is carried by the event itself.
        foreach (var product in Products(contract))
        {
            if (fact.Values.SingleOrDefault(value => value.TargetProperty == product.Left.Id)?.Value is not SemanticNumberValue left ||
                fact.Values.SingleOrDefault(value => value.TargetProperty == product.Right.Id)?.Value is not SemanticNumberValue right)
            {
                continue;
            }

            if (product.Primitive == SemanticPrimitiveType.DecimalNumber)
            {
                type.GetProperty(ProductMember(product))!.SetValue(instance, left.Value * right.Value);
            }
            else
            {
                try
                {
                    type.GetProperty(ProductMember(product))!.SetValue(instance, checked(decimal.ToInt64(left.Value) * decimal.ToInt64(right.Value)));
                }
                catch (OverflowException exception)
                {
                    throw new SemanticProjectionLoweringRefused(new OverflowException($"Event '{contract.Name}' product '{product.Name}' exceeds Stage's exact whole-number carrier (Int64).", exception));
                }
            }
        }

        return instance;
    }

    internal IReadOnlyList<SemanticProjectionProduct> Products(SemanticEventContract contract) =>
        SemanticProjectionProducts.For(contract, new SemanticApplicationContext(plan));

    internal Type For(SemanticId eventContract)
    {
        if (_events.TryGetValue(eventContract, out var existing))
        {
            return existing;
        }

        var contract = plan.Events[eventContract];
        var type = _factory.CreateEventType("Stage.Semantic.Events", $"Event_{_events.Count}", contract.ContractId.ToString(), contract.Properties.ToDictionary(property => property.Name, property => ResolveReadModel(property.Type))
            .Concat(Products(contract).Select(product => KeyValuePair.Create(ProductMember(product), product.Primitive == SemanticPrimitiveType.DecimalNumber ? typeof(decimal) : typeof(long))))
            .ToDictionary());
        _events.Add(eventContract, type);
        return type;
    }

    internal object? ReadModelValue(SemanticValue value, SemanticTypeReference reference) =>
        reference.IsCollection || reference.Kind == SemanticTypeReferenceKind.CompositeType
            ? JsonSerializer.Deserialize(SemanticRunContext.Canonical(value, reference, new SemanticApplicationContext(plan)), ResolveReadModel(reference))
            : ConvertValue(value, ResolveReadModel(reference));

    internal Type ResolveReadModel(SemanticTypeReference reference)
    {
        if (reference.IsCollection) return ResolveReadModel(reference with { IsCollection = false, IsOptional = false }).MakeArrayType();
        if (reference.Kind != SemanticTypeReferenceKind.CompositeType) return Resolve(reference);
        if (!_composites.TryGetValue(reference.Target, out var type))
        {
            var composite = plan.Model.Application.Types.Single(composite => composite.Id == reference.Target);
            type = _factory.CreateReadModelType("Stage.Semantic.Composites", $"Composite_{_composites.Count}", composite.Properties.ToDictionary(property => property.Name, property => ResolveReadModel(property.Type)));
            _composites.Add(reference.Target, type);
        }
        return type;
    }

    Type Resolve(SemanticTypeReference reference)
    {
        if (reference.IsCollection || reference.Kind == SemanticTypeReferenceKind.CompositeType) return typeof(JsonElement);
        var primitive = reference.Kind == SemanticTypeReferenceKind.Concept
            ? plan.Model.Application.Concepts.Single(concept => concept.Id == reference.Target).Primitive
            : reference.Primitive;
        var type = _clrTypes.GetValueOrDefault(primitive, typeof(object));
        return reference.IsOptional && type.IsValueType ? typeof(Nullable<>).MakeGenericType(type) : type;
    }
}
