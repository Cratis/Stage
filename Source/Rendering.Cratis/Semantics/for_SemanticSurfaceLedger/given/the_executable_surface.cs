// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSurfaceLedger.given;

public class the_executable_surface : Specification
{
    protected static IEnumerable<string> EnumerateSurface()
    {
        var assembly = typeof(ExecutableSemanticModel).Assembly;
        var types = assembly.GetExportedTypes().Where(type => type.Namespace == typeof(SemanticApplication).Namespace).ToArray();

        foreach (var member in EnumerateSupplementarySurface())
        {
            yield return member;
        }

        var pending = new Queue<Type>([typeof(ExecutableSemanticModel), typeof(SemanticApplication)]);
        var visited = new HashSet<Type>();
        while (pending.TryDequeue(out var type))
        {
            type = Unwrap(type);
            if (!types.Contains(type) || !visited.Add(type) || (!type.IsClass && !type.IsValueType))
            {
                continue;
            }

            if (type.IsEnum)
            {
                foreach (var member in Enum.GetNames(type))
                {
                    yield return $"{type.Name}.{member}";
                }

                continue;
            }

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (properties.Length == 0)
            {
                // An empty new expression/value variant must still be explicitly classified.
                yield return $"{type.Name}.$type";
            }

            foreach (var property in properties)
            {
                yield return $"{type.Name}.{property.Name}";
                pending.Enqueue(property.PropertyType);
            }

            if (type.IsAbstract)
            {
                foreach (var derived in types.Where(candidate => candidate.IsSubclassOf(type)))
                {
                    pending.Enqueue(derived);
                }
            }
        }
    }

    static IEnumerable<string> EnumerateSupplementarySurface()
    {
        // Compilation exposes implementation attachments beside (not inside) the executable model.
        // Audit their metadata separately without treating compiler source-map types as rendered input.
        foreach (var property in typeof(SemanticImplementationRequirement).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            yield return $"{nameof(SemanticImplementationRequirement)}.{property.Name}";
        }

        // Pending attachment state and the evaluator's body capability are not ESM members,
        // but both must retain an explicit refusal until described bodies are implemented.
        foreach (var member in Enum.GetNames<SemanticAttachmentResolution>())
        {
            yield return $"{nameof(SemanticAttachmentResolution)}.{member}";
        }
        yield return "SemanticExecutionCapability.ImplementationBody";

        // The application exposes these enums through Nullable<T>, outside the ordinary ESM traversal.
        foreach (var member in Enum.GetNames<SemanticCommandReadMode>())
        {
            yield return $"{nameof(SemanticCommandReadMode)}.{member}";
        }
        foreach (var member in Enum.GetNames<SemanticReactionProductionMode>())
        {
            yield return $"{nameof(SemanticReactionProductionMode)}.{member}";
        }
        foreach (var member in Enum.GetNames<SemanticTranslationDirection>())
        {
            yield return $"{nameof(SemanticTranslationDirection)}.{member}";
        }
    }

    static Type Unwrap(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ImmutableArray<>)
        ? type.GetGenericArguments()[0]
        : type;
}
