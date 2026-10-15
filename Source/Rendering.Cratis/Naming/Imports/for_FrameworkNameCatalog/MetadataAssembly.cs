// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_FrameworkNameCatalog;

/// <summary>
/// Reads the names a reference assembly contributes as metadata, through <see cref="System.Reflection.Metadata"/>:
/// the assembly is never loaded, so a reference for a different runtime or a reference-only assembly reads the same way.
/// </summary>
static class MetadataAssembly
{
    /// <summary>
    /// Reads an assembly.
    /// </summary>
    /// <param name="path">The assembly path.</param>
    /// <returns>Every namespace the assembly declares a type in, and its externally visible types.</returns>
    public static (IReadOnlyList<string> Namespaces, IReadOnlyList<MetadataType> Types) Read(string path)
    {
        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        var metadata = pe.GetMetadataReader();
        var namespaces = new SortedSet<string>(StringComparer.Ordinal);
        var types = new List<MetadataType>();
        foreach (var handle in metadata.TypeDefinitions)
        {
            var definition = metadata.GetTypeDefinition(handle);
            var name = metadata.GetString(definition.Name);
            if (name.Contains('<', StringComparison.Ordinal)) continue;
            if (definition.GetDeclaringType().IsNil)
            {
                // A namespace exists for lookup even when it only holds internal types.
                namespaces.Add(metadata.GetString(definition.Namespace));
            }

            if (IsVisible(metadata, definition)) types.Add(Type(metadata, definition));
        }

        namespaces.Remove(string.Empty);
        return ([.. namespaces], types);
    }

    static bool IsVisible(MetadataReader metadata, TypeDefinition definition)
    {
        var visibility = definition.Attributes & TypeAttributes.VisibilityMask;
        var declaring = definition.GetDeclaringType();
        if (declaring.IsNil) return visibility == TypeAttributes.Public;
        return visibility is TypeAttributes.NestedPublic && IsVisible(metadata, metadata.GetTypeDefinition(declaring));
    }

    static MetadataType Type(MetadataReader metadata, TypeDefinition definition)
    {
        var (@namespace, name) = Name(metadata, definition);
        var members = new SortedSet<string>(StringComparer.Ordinal);
        var extensions = new SortedSet<string>(StringComparer.Ordinal);
        var isExtensionContainer = HasExtensionAttribute(metadata, definition.GetCustomAttributes());
        foreach (var method in definition.GetMethods().Select(metadata.GetMethodDefinition).Where(method => IsInherited(method.Attributes & MethodAttributes.MemberAccessMask)))
        {
            var methodName = metadata.GetString(method.Name);
            if (isExtensionContainer && method.Attributes.HasFlag(MethodAttributes.Static) && !methodName.Contains('<', StringComparison.Ordinal))
            {
                // C# 14 extension properties compile to get_/set_ implementation methods named after the property.
                extensions.Add(methodName.StartsWith("get_", StringComparison.Ordinal) || methodName.StartsWith("set_", StringComparison.Ordinal) ? methodName[4..] : methodName);
            }

            if (!method.Attributes.HasFlag(MethodAttributes.SpecialName) && IsMemberName(methodName)) members.Add(methodName);
        }

        foreach (var property in definition.GetProperties().Select(metadata.GetPropertyDefinition))
        {
            var accessors = property.GetAccessors();
            var accessor = accessors.Getter.IsNil ? accessors.Setter : accessors.Getter;
            if (!accessor.IsNil && IsInherited(metadata.GetMethodDefinition(accessor).Attributes & MethodAttributes.MemberAccessMask)) members.Add(metadata.GetString(property.Name));
        }

        foreach (var field in definition.GetFields().Select(metadata.GetFieldDefinition))
        {
            var access = field.Attributes & FieldAttributes.FieldAccessMask;
            if (access is FieldAttributes.Public or FieldAttributes.Family or FieldAttributes.FamORAssem && !field.Attributes.HasFlag(FieldAttributes.SpecialName)) members.Add(metadata.GetString(field.Name));
        }

        foreach (var @event in definition.GetEvents().Select(metadata.GetEventDefinition))
        {
            members.Add(metadata.GetString(@event.Name));
        }

        members.RemoveWhere(member => !IsMemberName(member));
        var nested = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var type in definition.GetNestedTypes().Select(metadata.GetTypeDefinition))
        {
            var access = type.Attributes & TypeAttributes.VisibilityMask;
            var nestedName = metadata.GetString(type.Name);
            if (access is TypeAttributes.NestedPublic or TypeAttributes.NestedFamily or TypeAttributes.NestedFamORAssem && IsMemberName(nestedName)) nested.Add(CatalogNames.SimpleName(nestedName));
        }

        return new(@namespace, name, definition.Attributes, BaseName(metadata, definition.BaseType), [.. members], [.. nested], [.. extensions]);
    }

    static bool IsInherited(MethodAttributes access) => access is MethodAttributes.Public or MethodAttributes.Family or MethodAttributes.FamORAssem;

    static bool IsMemberName(string name) => !name.Contains('<', StringComparison.Ordinal) && !name.Contains('.', StringComparison.Ordinal);

    static bool HasExtensionAttribute(MetadataReader metadata, CustomAttributeHandleCollection attributes) =>
        attributes.Select(metadata.GetCustomAttribute).Any(attribute => AttributeTypeName(metadata, attribute) == "System.Runtime.CompilerServices.ExtensionAttribute");

    static string AttributeTypeName(MetadataReader metadata, CustomAttribute attribute) => attribute.Constructor.Kind switch
    {
        HandleKind.MemberReference => BaseName(metadata, metadata.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent),
        HandleKind.MethodDefinition => Full(Name(metadata, metadata.GetTypeDefinition(metadata.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType()))),
        _ => string.Empty
    };

    static string BaseName(MetadataReader metadata, EntityHandle handle) => handle.IsNil ? string.Empty : handle.Kind switch
    {
        HandleKind.TypeDefinition => Full(Name(metadata, metadata.GetTypeDefinition((TypeDefinitionHandle)handle))),
        HandleKind.TypeReference => Full(Name(metadata, metadata.GetTypeReference((TypeReferenceHandle)handle))),
        HandleKind.TypeSpecification => GenericDefinition(metadata, metadata.GetTypeSpecification((TypeSpecificationHandle)handle)),
        _ => string.Empty
    };

    static string GenericDefinition(MetadataReader metadata, TypeSpecification specification)
    {
        var blob = metadata.GetBlobReader(specification.Signature);
        if (blob.ReadSignatureTypeCode() != SignatureTypeCode.GenericTypeInstance) return string.Empty;
        blob.ReadSignatureTypeCode();
        return BaseName(metadata, blob.ReadTypeHandle());
    }

    static string Full((string Namespace, string Name) name) => CatalogNames.Qualify(name.Namespace, name.Name);

    static (string Namespace, string Name) Name(MetadataReader metadata, TypeDefinition definition)
    {
        var declaring = definition.GetDeclaringType();
        if (declaring.IsNil) return (metadata.GetString(definition.Namespace), metadata.GetString(definition.Name));
        var outer = Name(metadata, metadata.GetTypeDefinition(declaring));
        return (outer.Namespace, $"{outer.Name}+{metadata.GetString(definition.Name)}");
    }

    static (string Namespace, string Name) Name(MetadataReader metadata, System.Reflection.Metadata.TypeReference reference)
    {
        if (reference.ResolutionScope.Kind != HandleKind.TypeReference) return (metadata.GetString(reference.Namespace), metadata.GetString(reference.Name));
        var outer = Name(metadata, metadata.GetTypeReference((TypeReferenceHandle)reference.ResolutionScope));
        return (outer.Namespace, $"{outer.Name}+{metadata.GetString(reference.Name)}");
    }
}
