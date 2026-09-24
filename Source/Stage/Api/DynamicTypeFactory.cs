// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using Cratis.Chronicle.Events;

namespace Cratis.Stage.Api;

/// <summary>
/// Emits a distinct runtime CLR type per modeled command and read model. Arc keys command dispatch on the
/// command's runtime type and identifies query performers by their read model type, so each artifact needs its
/// own real type even though the model only carries a JSON schema.
/// </summary>
public sealed class DynamicTypeFactory
{
    readonly ModuleBuilder _module;
    readonly Dictionary<string, Type> _types = [];
    readonly Lock _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicTypeFactory"/> class.
    /// </summary>
    public DynamicTypeFactory()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Stage.Generated"), AssemblyBuilderAccess.Run);
        _module = assembly.DefineDynamicModule("Stage.Generated");
    }

    /// <summary>
    /// Creates a runtime command type that binds the request body into <see cref="DynamicCommand.Data"/>.
    /// </summary>
    /// <param name="namespace">The namespace for the emitted type.</param>
    /// <param name="name">The simple name of the command.</param>
    /// <returns>The emitted command type.</returns>
    public Type CreateCommandType(string @namespace, string name) => CreateType($"{@namespace}.{name}", typeof(DynamicCommand));

    /// <summary>
    /// Creates a runtime read model type that uniquely identifies a modeled read model to Arc.
    /// </summary>
    /// <param name="namespace">The namespace for the emitted type.</param>
    /// <param name="name">The simple name of the read model.</param>
    /// <returns>The emitted read model type.</returns>
    public Type CreateReadModelType(string @namespace, string name) => CreateType($"{@namespace}.{name}", typeof(DynamicReadModel));

    /// <summary>
    /// Creates a CLR event contract with typed properties and a stable Chronicle event type identity.
    /// </summary>
    /// <param name="namespace">The emitted namespace.</param>
    /// <param name="name">The event name.</param>
    /// <param name="eventTypeId">The persisted event contract identity.</param>
    /// <param name="properties">The named CLR property types.</param>
    /// <returns>The emitted CLR type.</returns>
    public Type CreateEventType(string @namespace, string name, string eventTypeId, IReadOnlyDictionary<string, Type> properties) =>
        CreateTypedType($"{@namespace}.{name}", properties, eventTypeId);

    /// <summary>
    /// Creates a CLR read model with typed properties.
    /// </summary>
    /// <param name="namespace">The emitted namespace.</param>
    /// <param name="name">The read-model name.</param>
    /// <param name="properties">The named CLR property types.</param>
    /// <returns>The emitted CLR type.</returns>
    public Type CreateReadModelType(string @namespace, string name, IReadOnlyDictionary<string, Type> properties) =>
        CreateTypedType($"{@namespace}.{name}", properties, null);

    Type CreateTypedType(string fullName, IReadOnlyDictionary<string, Type> properties, string? eventTypeId)
    {
        lock (_lock)
        {
            if (_types.TryGetValue(fullName, out var existing))
            {
                return existing;
            }

            var builder = _module.DefineType(fullName, TypeAttributes.Public);
            builder.DefineDefaultConstructor(MethodAttributes.Public);
            if (eventTypeId is not null)
            {
                var constructor = typeof(EventTypeAttribute).GetConstructor([typeof(string), typeof(uint)])!;
                builder.SetCustomAttribute(new CustomAttributeBuilder(constructor, [eventTypeId, 1u]));
            }

            foreach (var (propertyName, propertyType) in properties)
            {
                var field = builder.DefineField($"_{propertyName}", propertyType, FieldAttributes.Private);
                var property = builder.DefineProperty(propertyName, PropertyAttributes.None, propertyType, null);
                var getter = builder.DefineMethod($"get_{propertyName}", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
                var getIl = getter.GetILGenerator();
                getIl.Emit(OpCodes.Ldarg_0);
                getIl.Emit(OpCodes.Ldfld, field);
                getIl.Emit(OpCodes.Ret);
                var setter = builder.DefineMethod($"set_{propertyName}", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, null, [propertyType]);
                var setIl = setter.GetILGenerator();
                setIl.Emit(OpCodes.Ldarg_0);
                setIl.Emit(OpCodes.Ldarg_1);
                setIl.Emit(OpCodes.Stfld, field);
                setIl.Emit(OpCodes.Ret);
                property.SetGetMethod(getter);
                property.SetSetMethod(setter);
            }

            var type = builder.CreateType();
            _types[fullName] = type;
            return type;
        }
    }

    Type CreateType(string fullName, Type baseType)
    {
        lock (_lock)
        {
            // The same artifact can be requested more than once because Arc resolves the discovered providers
            // multiple times; reuse the already-emitted type rather than emitting a duplicate name.
            if (_types.TryGetValue(fullName, out var existing))
            {
                return existing;
            }

            // TypeAttributes.Class is zero — naming it beside Public says nothing the default does not already
            // say, and Roslynator flags the redundant flag as an error in Release.
            var typeBuilder = _module.DefineType(fullName, TypeAttributes.Public, baseType);

            // Emit a public parameterless constructor chaining to the base so the JSON deserializer can instantiate it.
            var constructor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
            var il = constructor.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, baseType.GetConstructor(Type.EmptyTypes)!);
            il.Emit(OpCodes.Ret);

            var type = typeBuilder.CreateType();
            _types[fullName] = type;

            return type;
        }
    }
}
