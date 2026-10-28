// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Execution;

namespace Cratis.Stage.Rendering.Cratis.Execution;

/// <summary>Materializes only a rendered wrapper's declared constructor types inside the worker.</summary>
internal static class BodyValueMaterializer
{
    static readonly Dictionary<Type, Func<string, object>> _textReaders = new()
    {
        [typeof(string)] = value => value,
        [typeof(Guid)] = value => Guid.Parse(value),
        [typeof(DateOnly)] = value => DateOnly.Parse(value, CultureInfo.InvariantCulture),
        [typeof(TimeOnly)] = value => TimeOnly.Parse(value, CultureInfo.InvariantCulture),
        [typeof(DateTimeOffset)] = value => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture)
    };

    internal static object Wrapper(Type type, BodyEvaluationRequest request, BodyUnitEntry unit)
    {
        var constructor = type.GetConstructors().Single();
        return constructor.Invoke([.. constructor.GetParameters().Select(parameter => request.Members.TryGetValue(parameter.Name!, out var value)
            ? Convert(SemanticValueWire.Read(value), parameter.ParameterType, unit)
            : throw new ArgumentException("Missing typed context member."))]);
    }

    static object? Convert(SemanticValue value, Type type, BodyUnitEntry unit)
    {
        if (value is SemanticNullValue) return Null(type);
        return Required(value, Nullable.GetUnderlyingType(type) ?? type, unit);
    }

    static object Required(SemanticValue value, Type type, BodyUnitEntry unit) =>
        value switch
        {
            SemanticTextValue text => Text(text, type, unit),
            SemanticBooleanValue boolean when type == typeof(bool) => boolean.Value,
            SemanticNumberValue number => Numeric(number, type, unit),
            SemanticArrayValue array => Collection(array, type, unit),
            SemanticCompositeValue composite => Composite(composite, type, unit),
            _ => Concept(value, type, unit)
        };

    static object Numeric(SemanticNumberValue number, Type type, BodyUnitEntry unit)
    {
        if (type.IsPrimitive || type == typeof(decimal)) return Number(number.Value, type);
        if (type.IsEnum) return Enum.ToObject(type, checked((int)number.Value));
        return Concept(number, type, unit);
    }

    static object? Null(Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) is not null ? null : throw new ArgumentException("Null cannot be materialized as a required value type.");

    static object Text(SemanticTextValue text, Type type, BodyUnitEntry unit)
    {
        if (_textReaders.TryGetValue(type, out var parse)) return parse(text.Value);
        return type.IsEnum ? Enum.Parse(type, text.Value, ignoreCase: false) : Concept(text, type, unit);
    }

    static object Number(decimal number, Type type)
    {
        var fractional = type == typeof(decimal) || type == typeof(double) || type == typeof(float);
        if (!fractional && number != decimal.Truncate(number)) throw new ArgumentException("A whole-number context value cannot have a fractional part.");
        return System.Convert.ChangeType(number, type, CultureInfo.InvariantCulture);
    }

    static object Composite(SemanticCompositeValue composite, Type type, BodyUnitEntry unit)
    {
        var constructor = type.GetConstructors().Single();
        var properties = composite.Properties.ToDictionary(property => unit.PropertyNames.TryGetValue(property.TargetProperty.ToString(), out var name) ? name : throw new ArgumentException("Unknown composite property."), StringComparer.OrdinalIgnoreCase);
        return constructor.Invoke([.. constructor.GetParameters().Select(parameter => properties.TryGetValue(parameter.Name!, out var property)
            ? Convert(property.Value, parameter.ParameterType, unit) : throw new ArgumentException("Missing composite property."))]);
    }

    static object Concept(SemanticValue value, Type type, BodyUnitEntry unit)
    {
        var constructor = type.GetConstructors().Single();
        var parameters = constructor.GetParameters();
        if (parameters.Length != 1 || !string.Equals(parameters[0].Name, "Value", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The portable value does not match the rendered type.");
        return constructor.Invoke([Convert(value, parameters[0].ParameterType, unit)]);
    }

    static object Collection(SemanticArrayValue value, Type type, BodyUnitEntry unit)
    {
        var element = type.IsArray ? type.GetElementType()! : type.GetGenericArguments().Single();
        var array = Array.CreateInstance(element, value.Values.Length);
        for (var index = 0; index < value.Values.Length; index++) array.SetValue(Convert(value.Values[index], element, unit), index);
        if (type.IsAssignableFrom(array.GetType())) return array;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ImmutableArray<>)) return Immutable(array, element);
        throw new ArgumentException("The rendered collection type is unsupported.");
    }

    static object Immutable(Array array, Type element)
    {
        var method = typeof(ImmutableArray).GetMethods().Single(IsRangeMethod);
        return method.MakeGenericMethod(element).Invoke(null, [array])!;
    }

    static bool IsRangeMethod(MethodInfo method) =>
        method.Name == "CreateRange" && method.IsGenericMethodDefinition && method.GetParameters().Length == 1 &&
        method.GetParameters()[0].ParameterType.IsGenericType && method.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>);
}
