// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;

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

            var typeBuilder = _module.DefineType(fullName, TypeAttributes.Public | TypeAttributes.Class, baseType);

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
