// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>The generated declarations that authenticated bodies are compiled against, shared by every body analysis.</summary>
internal static class SemanticBodyDeclarations
{
    /// <summary>Declares each event as a record in its declaring slice namespace.</summary>
    /// <param name="context">The application being rendered.</param>
    /// <param name="events">The events to declare.</param>
    /// <returns>One namespace declaration per event.</returns>
    internal static IEnumerable<string> Events(SemanticApplicationContext context, IEnumerable<SemanticEventContract> events)
    {
        var types = new SemanticTypeSystem(context);
        return events.Select(@event => $"namespace {SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(@event.Id).Path)} {{ public record {Identifiers.ToPascalCase(@event.Name)}({string.Join(", ", @event.Properties.Select(property => $"{types.Type(property.Type)} {Identifiers.ToPascalCase(property.Name)}"))}); }}");
    }

    /// <summary>The namespaces that hold the events, plus the common namespace.</summary>
    /// <param name="context">The application being rendered.</param>
    /// <param name="events">The events whose namespaces are wanted.</param>
    /// <returns>The distinct namespaces.</returns>
    internal static IEnumerable<string> EventNamespaces(SemanticApplicationContext context, IEnumerable<SemanticEventContract> events) =>
        events.Select(@event => SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(@event.Id).Path)).Append(context.CommonNamespace).Distinct(StringComparer.Ordinal);

    /// <summary>The real generated concept declarations, with their implicit conversions.</summary>
    /// <param name="context">The application being rendered.</param>
    /// <returns>One declaration per concept.</returns>
    internal static IEnumerable<string> RealConcepts(SemanticApplicationContext context) =>
        context.Application.Concepts.Select(concept => SemanticCommonArtifactRenderer.Render(concept, context, qualified: true, declarationOnly: true).Content);

    /// <summary>The composite type records in the common namespace.</summary>
    /// <param name="context">The application being rendered.</param>
    /// <returns>One declaration per composite type.</returns>
    internal static IEnumerable<string> CompositeTypes(SemanticApplicationContext context)
    {
        var types = new SemanticTypeSystem(context);
        return context.Application.Types.Select(composite => $"public record {Identifiers.ToPascalCase(composite.Name)}({string.Join(", ", composite.Properties.Select(property => $"{types.Type(property.Type)} {Identifiers.ToPascalCase(property.Name)}"))});");
    }

    /// <summary>The command input records in their slice namespaces.</summary>
    /// <param name="context">The application being rendered.</param>
    /// <returns>The command record declarations.</returns>
    internal static string Commands(SemanticApplicationContext context)
    {
        var types = new SemanticTypeSystem(context);
        var code = string.Empty;
        foreach (var command in context.Commands.Values)
        {
            var raw = SemanticDateComparisonAdmission.Raw(context.Application, command);
            var inputs = command.Properties.Where(property => !property.IsGenerated).Select(property =>
                $"{SemanticCommandResultRendering.InputType(property, raw, types)} {context.MemberNames.Property(property)}");
            code += $"namespace {SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(command.Id).Path)} {{ public record {Identifiers.ToPascalCase(command.Name)}({string.Join(", ", inputs)}); }}\n";
        }

        return code;
    }
}
