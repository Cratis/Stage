// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Schemas;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Specifications.Types;

/// <summary>
/// Emits the event property types for composite and collection values: a <see cref="SemanticStructuredEventValue"/>
/// subclass whose Chronicle schema comes from a schema-only shape of nullable members.
/// </summary>
/// <param name="plan">The semantic contract plan.</param>
internal sealed class SemanticStructuredEventValueTypes(SemanticExecutionPlan plan)
{
    readonly ModuleBuilder _module = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Stage.Generated.EventValues"), AssemblyBuilderAccess.Run).DefineDynamicModule("Stage.Generated.EventValues");
    readonly Dictionary<SemanticId, Type> _shapes = [];
    readonly Dictionary<Type, Type> _wrappers = [];

    /// <summary>
    /// Gets the event property type that carries a structured value, or <see langword="null"/> when the type is recursive.
    /// </summary>
    /// <param name="type">The structured type reference.</param>
    /// <returns>The wrapper type, or <see langword="null"/> when the value has no finite shape.</returns>
    internal Type? For(SemanticTypeReference type)
    {
        if (!SemanticStructuredTypes.HasFiniteShape(plan, type))
        {
            return null;
        }

        var shape = Shape(type);
        if (!_wrappers.TryGetValue(shape, out var wrapper))
        {
            wrapper = Emit($"Stage.Semantic.EventValues.Value_{_wrappers.Count}", shape);
            _wrappers.Add(shape, wrapper);
        }

        return wrapper;
    }

    Type Shape(SemanticTypeReference type)
    {
        if (type.IsCollection)
        {
            return Shape(type with { IsCollection = false }).MakeArrayType();
        }

        if (type.Kind != SemanticTypeReferenceKind.CompositeType)
        {
            return PrimitiveShape(type);
        }

        if (!_shapes.TryGetValue(type.Target, out var shape))
        {
            var composite = plan.Model.Application.Types.Single(candidate => candidate.Id == type.Target);
            var members = composite.Properties.ToDictionary(property => property.Name, property => Shape(property.Type));
            shape = EmitShape($"Stage.Semantic.EventShapes.Shape_{_shapes.Count}", members);
            _shapes.Add(type.Target, shape);
        }

        return shape;
    }

    // Numbers are read as decimal so every whole number in the long range and every authored decimal is exact;
    // everything else stays text, so Chronicle never converts it into a Guid or a date. All members are nullable so
    // an absent or null member never turns into 0 or false.
    Type PrimitiveShape(SemanticTypeReference type)
    {
        var primitive = type.Kind == SemanticTypeReferenceKind.Concept
            ? plan.Model.Application.Concepts.Single(concept => concept.Id == type.Target).Primitive
            : type.Primitive;
        return primitive switch
        {
            SemanticPrimitiveType.DecimalNumber or SemanticPrimitiveType.WholeNumber => typeof(decimal?),
            SemanticPrimitiveType.Boolean => typeof(bool?),
            _ => typeof(string)
        };
    }

    // The shapes live in the same dynamic assembly as the wrappers: an attribute can only name a type that its own assembly resolves.
    Type EmitShape(string fullName, Dictionary<string, Type> members)
    {
        var builder = _module.DefineType(fullName, TypeAttributes.Public);
        builder.DefineDefaultConstructor(MethodAttributes.Public);
        foreach (var (name, memberType) in members)
        {
            var field = builder.DefineField($"_{name}", memberType, FieldAttributes.Private);
            var property = builder.DefineProperty(name, PropertyAttributes.None, memberType, null);
            const MethodAttributes accessor = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
            var getter = builder.DefineMethod($"get_{name}", accessor, memberType, Type.EmptyTypes);
            var getIl = getter.GetILGenerator();
            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, field);
            getIl.Emit(OpCodes.Ret);
            var setter = builder.DefineMethod($"set_{name}", accessor, null, [memberType]);
            var setIl = setter.GetILGenerator();
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, field);
            setIl.Emit(OpCodes.Ret);
            property.SetGetMethod(getter);
            property.SetSetMethod(setter);
        }

        return builder.CreateType();
    }

    Type Emit(string fullName, Type shape)
    {
        var builder = _module.DefineType(fullName, TypeAttributes.Public | TypeAttributes.Sealed, typeof(SemanticStructuredEventValue));
        var constructor = builder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
        var il = constructor.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, typeof(SemanticStructuredEventValue).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, Type.EmptyTypes)!);
        il.Emit(OpCodes.Ret);
        builder.SetCustomAttribute(new CustomAttributeBuilder(typeof(JsonConverterAttribute).GetConstructor([typeof(Type)])!, [typeof(SemanticStructuredEventValueConverterFactory)]));
        builder.SetCustomAttribute(typeof(JsonSchemaTypeAttribute).GetConstructor([typeof(Type)])!, SchemaTypeBlob(shape));
        return builder.CreateType();
    }

    // CustomAttributeBuilder names a Type argument with its assembly, which a dynamic assembly cannot be loaded by.
    // The blob names this assembly's own types without an assembly, so the runtime resolves them from the wrapper's assembly.
    byte[] SchemaTypeBlob(Type shape)
    {
        var element = shape.IsArray ? shape.GetElementType()! : shape;
        var name = _shapes.ContainsValue(element) ? $"{element.FullName}{(shape.IsArray ? "[]" : string.Empty)}" : shape.AssemblyQualifiedName!;
        var text = System.Text.Encoding.UTF8.GetBytes(name);
        using var blob = new MemoryStream();
        blob.Write([0x01, 0x00]);
        blob.Write(text.Length < 0x80 ? [(byte)text.Length] : [(byte)(0x80 | (text.Length >> 8)), (byte)(text.Length & 0xFF)]);
        blob.Write(text);
        blob.Write([0x00, 0x00]);
        return blob.ToArray();
    }
}
