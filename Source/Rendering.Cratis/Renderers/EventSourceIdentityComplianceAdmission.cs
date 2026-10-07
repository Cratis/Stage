// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// Refuses protected concept identities before the syntax renderer publishes any output.
/// </summary>
internal static class EventSourceIdentityComplianceAdmission
{
    /// <summary>
    /// Checks every identity used by the selected slices.
    /// </summary>
    /// <param name="slices">The selected slices.</param>
    /// <param name="context">The surrounding declarations.</param>
    public static void EnsureAccepted(IEnumerable<LocatedSlice> slices, ApplicationSet context)
    {
        foreach (var (type, use, location) in IdentityUses(slices, context))
        {
            if (context.Concepts.TryGetValue(type.Name, out var concept))
            {
                EnsureUnprotected(concept, use, location);
            }
        }
    }

    /// <summary>
    /// Checks a directly rendered concept against all identity declarations in its context.
    /// </summary>
    /// <param name="concept">The concept being rendered.</param>
    /// <param name="context">The surrounding declarations.</param>
    public static void EnsureAccepted(ConceptSyntax concept, ApplicationSet context)
    {
        if (context.IdentifierConceptNames.Contains(concept.Name))
        {
            EnsureUnprotected(concept, "an event-source identity", concept.Location);
        }

        foreach (var (_, use, location) in IdentityUses(context.Slices, context).Where(identity => identity.Type.Name == concept.Name))
        {
            EnsureUnprotected(concept, use, location);
        }
    }

    static void EnsureUnprotected(ConceptSyntax concept, string use, SourceLocation location)
    {
        var attribute = concept.Attributes.Select(attribute => attribute.Name).FirstOrDefault(name => string.Equals(name, "pii", StringComparison.Ordinal) || string.Equals(name, "sensitive", StringComparison.Ordinal));
        if (attribute is not null)
        {
            throw new UnsupportedProtectedEventSourceIdentity(concept.Name, attribute, use, location);
        }
    }

    static IEnumerable<(TypeRefSyntax Type, string Use, SourceLocation Location)> IdentityUses(IEnumerable<LocatedSlice> slices, ApplicationSet context)
    {
        foreach (var slice in slices)
        {
            var slicePath = string.Join('.', slice.FullPath);
            var properties = slice.Slice.Commands.SelectMany(command => command.Properties)
                .Concat(slice.Slice.Events.SelectMany(@event => @event.Properties))
                .Concat((slice.Slice.ReadModels ?? []).SelectMany(readModel => readModel.Properties));
            foreach (var property in properties.Where(property => context.IdentifierConceptNames.Contains(property.Type.Name)))
            {
                yield return (property.Type, $"an identity concept referenced in slice '{slicePath}'", property.Location);
            }

            foreach (var command in slice.Slice.Commands)
            {
                foreach (var property in command.Properties.Where(property => property.IsIdentifier))
                {
                    yield return (property.Type, $"command identifier '{command.Name}.{property.Name}' in slice '{slicePath}'", property.Location);
                }

                foreach (var produced in command.Produces.Where(produced => produced.For is PathExpressionSyntax))
                {
                    var path = (PathExpressionSyntax)produced.For!;
                    if (TypeAtPath(command.Properties, path.Path, context) is { } type)
                    {
                        yield return (type, $"production destination '{path.Path}' of '{produced.Event}' in slice '{slicePath}'", path.Location);
                    }
                }
            }

            foreach (var projection in slice.Slice.Projections)
            {
                foreach (var from in FromBlocks(projection.Blocks))
                {
                    foreach (var spec in from.Events)
                    {
                        var keys = new[] { spec.Key, (from.Key as ExpressionKeySyntax)?.Expression, (projection.Key as ExpressionKeySyntax)?.Expression };
                        foreach (var key in keys.OfType<PathExpressionSyntax>())
                        {
                            if (context.Events.TryGetValue(spec.Event, out var declared) && TypeAtPath(declared.Properties, key.Path, context) is { } type)
                            {
                                yield return (type, $"projection key '{projection.Name}.{key.Path}' in slice '{slicePath}'", key.Location);
                            }
                        }
                    }
                }
            }
        }
    }

    static IEnumerable<FromSyntax> FromBlocks(IEnumerable<ProjectionBlockSyntax> blocks) => blocks.SelectMany(block => block switch
    {
        FromSyntax from => Enumerable.Repeat(from, 1),
        NestedSyntax nested => FromBlocks(nested.Blocks),
        ChildrenSyntax children => FromBlocks(children.Blocks),
        _ => [],
    });

    static TypeRefSyntax? TypeAtPath(IEnumerable<PropertySyntax> properties, string path, ApplicationSet context)
    {
        TypeRefSyntax? type = null;
        foreach (var segment in path.Split('.'))
        {
            type = properties.FirstOrDefault(property => string.Equals(property.Name, segment, StringComparison.OrdinalIgnoreCase))?.Type;
            properties = type is not null && context.Types.TryGetValue(type.Name, out var composite) ? composite.Properties : [];
        }

        return type;
    }
}
