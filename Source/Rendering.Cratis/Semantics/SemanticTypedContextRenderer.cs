// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Generates a distinct statically typed C# context for each requirement and use site.</summary>
internal static class SemanticTypedContextRenderer
{
    internal static RenderedFile Render(SemanticTypedContextDescriptor descriptor, SemanticApplicationContext context)
    {
        var suffix = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{descriptor.RequirementId}:{descriptor.OperationId}")))[..16];
        var name = $"TypedContext_{suffix}";
        var builder = new CSharpCodeBuilder().Namespace($"{context.RootNamespace}.TypedContexts");
        var definitions = descriptor.Types.ToDictionary(definition => definition.Id);
        foreach (var definition in definitions.Values)
        {
            switch (definition.Kind)
            {
                case SemanticTypeReferenceKind.Concept when context.Concepts.TryGetValue(definition.Id, out var concept) &&
                    concept.Name == definition.Name && concept.Primitive == definition.Primitive && definition.Properties.IsEmpty:
                    break;
                case SemanticTypeReferenceKind.CompositeType when context.Types.TryGetValue(definition.Id, out var composite) &&
                    composite.Name == definition.Name && definition.Primitive == SemanticPrimitiveType.Unknown &&
                    SameProperties(definition.Properties, composite.Properties):
                    break;
                default:
                    throw Rejected($"Type definition '{definition.Id}' does not match the model.");
            }
        }

        var members = new List<string>();
        var derived = new List<string>();
        var localShapes = new Dictionary<SemanticId, (string Name, SemanticContextProperty[] Properties)>();
        foreach (var member in descriptor.Members)
        {
            if (member.Source is null || !KnownSource(member.Source.Kind))
                throw Rejected($"Member '{member.Name}' has an unknown source kind.");
            var type = Resolve(member.Type, member.Name);
            if (member.IsNullable && !type.EndsWith('?')) type += "?";
            if (member.IsDerived)
            {
                if (member.Name == "IsFirst" && member.Source.Kind == SemanticContextSourceKinds.Derived && member.Source.Path == "State" && type == "bool")
                    derived.Add("public bool IsFirst => State is null;");
                else if (member.Name == "IsWholeArtifact" && member.Source.Kind == SemanticContextSourceKinds.Derived && member.Source.Path == "Property" && type == "bool")
                    derived.Add("public bool IsWholeArtifact => string.IsNullOrEmpty(Property);");
                else throw Rejected($"Derived member '{member.Name}' has no admitted C# implementation.");
            }
            else
            {
                members.Add($"{type} {Identifiers.EscapeKeyword(member.Name)}");
            }
        }

        foreach (var declaration in localShapes.Values)
        {
            builder.Line($"public record {declaration.Name}({string.Join(", ", declaration.Properties.Select(property => $"{ModelType(property.Type)} {Identifiers.ToPascalCase(property.Name)}"))});")
                .BlankLine();
        }
        builder.OpenBlock($"public record {name}({string.Join(", ", members)})");
        foreach (var expression in derived) builder.Line(expression);
        builder.EndBlock();
        return new(Path.Combine("TypedContexts", $"{name}.cs"), builder.ToString())
        {
            Sources = descriptor.OperationId is { } operation ? [operation] : []
        };

        string Resolve(SemanticContextType type, string memberName)
        {
            if (type is null) throw Rejected($"Member '{memberName}' has no type.");
            return type.Kind switch
            {
                SemanticContextTypeKinds.Runtime when type.ModelType is null && type.Shape is null && type.Properties.IsEmpty =>
                    Runtime(type.RuntimeToken),
                SemanticContextTypeKinds.Model when type.ModelType is not null && type.Shape is null && type.RuntimeToken is null && type.Properties.IsEmpty =>
                    ModelType(type.ModelType),
                SemanticContextTypeKinds.Shape when type.Shape is { } shape && type.ModelType is null && type.RuntimeToken is null =>
                    ShapeType(shape, type.Properties, memberName),
                _ => throw Rejected($"Member '{memberName}' has an unknown or inconsistent type kind '{type.Kind}'.")
            };
        }

        string ShapeType(SemanticId id, System.Collections.Immutable.ImmutableArray<SemanticContextProperty> properties, string memberName)
        {
            if (context.Commands.TryGetValue(id, out var command))
            {
                if (!SameProperties(properties, command.Properties)) throw Rejected($"Shape '{id}' differs from command properties.");
                return SliceType(id, command.Name);
            }
            if (context.Events.TryGetValue(id, out var @event))
            {
                if (!SameProperties(properties, @event.Properties)) throw Rejected($"Shape '{id}' differs from current event properties.");
                return SliceType(id, @event.Name);
            }
            if (context.ReadModels.TryGetValue(id, out var readModel))
            {
                if (!SameProperties(properties, readModel.Properties)) throw Rejected($"Shape '{id}' differs from read-model properties.");
                return SliceType(id, readModel.Name);
            }
            if (context.Queries.TryGetValue(id, out var query))
            {
                var expected = new SemanticContextProperty(query.Argument.Name, query.Argument.Id, query.Argument.Type);
                if (properties.Length != 1 || properties[0] != expected) throw Rejected($"Shape '{id}' differs from query arguments.");
                var localName = $"{name}_{Identifiers.ToPascalCase(memberName)}Shape";
                localShapes.Add(id, (localName, [.. properties]));
                return localName;
            }
            throw Rejected($"Shape '{id}' does not name a generated command, event, read model or query.");
        }

        string SliceType(SemanticId id, string typeName) =>
            $"global::{SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(id).Path)}.{Identifiers.ToPascalCase(typeName)}";

        string ModelType(SemanticTypeReference reference)
        {
            if (reference is null) throw Rejected("A model property has no type.");
            var scalar = reference.Kind switch
            {
                SemanticTypeReferenceKind.Primitive when reference.Primitive is SemanticPrimitiveType.Uuid or SemanticPrimitiveType.Text or SemanticPrimitiveType.WholeNumber or SemanticPrimitiveType.DecimalNumber or SemanticPrimitiveType.Boolean or SemanticPrimitiveType.Date or SemanticPrimitiveType.DateTime => SemanticTypeSystem.Primitive(reference.Primitive),
                SemanticTypeReferenceKind.Concept when definitions.TryGetValue(reference.Target, out var definition) &&
                    definition.Kind == reference.Kind => $"global::{context.RootNamespace}.Common.{Identifiers.ToPascalCase(definition.Name)}",
                SemanticTypeReferenceKind.CompositeType when definitions.TryGetValue(reference.Target, out var definition) &&
                    definition.Kind == reference.Kind => $"global::{context.RootNamespace}.Common.{Identifiers.ToPascalCase(definition.Name)}",
                _ => throw Rejected($"Model type '{reference.Kind}' / '{reference.Target}' has no matching definition.")
            };
            var type = reference.IsCollection ? $"IReadOnlyList<{scalar}>" : scalar;
            return reference.IsOptional ? $"{type}?" : type;
        }
    }

    static bool SameProperties(System.Collections.Immutable.ImmutableArray<SemanticContextProperty> actual, System.Collections.Immutable.ImmutableArray<SemanticProperty> expected) =>
        !actual.IsDefault && actual.Length == expected.Length && actual.Zip(expected).All(pair =>
            pair.First.Id == pair.Second.Id && pair.First.Name == pair.Second.Name && pair.First.Type == pair.Second.Type);

    static string Runtime(string? token) => token switch
    {
        SemanticContextRuntimeTokens.Text => "string",
        SemanticContextRuntimeTokens.WholeNumber => "long",
        SemanticContextRuntimeTokens.Boolean => "bool",
        SemanticContextRuntimeTokens.DateTime => "DateTimeOffset",
        SemanticContextRuntimeTokens.TenantId => "global::Cratis.Screenplay.Contexts.TenantId",
        SemanticContextRuntimeTokens.Identity => "global::Cratis.Screenplay.Contexts.Identity",
        SemanticContextRuntimeTokens.CausedBy => "global::Cratis.Screenplay.Contexts.CausedBy",
        SemanticContextRuntimeTokens.Causation => "global::Cratis.Screenplay.Contexts.Causation",
        _ => throw Rejected($"Unknown portable runtime token '{token}'.")
    };

    static bool KnownSource(string kind) => new[]
    {
        SemanticContextSourceKinds.ContextContract, SemanticContextSourceKinds.Derived, SemanticContextSourceKinds.Command,
        SemanticContextSourceKinds.ReadModel, SemanticContextSourceKinds.CurrentEvent, SemanticContextSourceKinds.EventSourceId,
        SemanticContextSourceKinds.ModelProperty, SemanticContextSourceKinds.ConceptValue, SemanticContextSourceKinds.ValidatedProperty,
        SemanticContextSourceKinds.ValidatedArtifact, SemanticContextSourceKinds.AuthorizedOperation,
        SemanticContextSourceKinds.CommandIdentifier, SemanticContextSourceKinds.QueryKey, SemanticContextSourceKinds.Unavailable
    }.Contains(kind, StringComparer.Ordinal);

    static InvalidTypedContext Rejected(string reason) => new($"Typed context cannot be rendered: {reason}");
}
