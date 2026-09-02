// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Types;

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// Resolves which read-model property carries a projection's <c>key</c>.
/// </summary>
/// <remarks>
/// A Screenplay <c>key</c> names an <b>event</b> property, while <c>[Key]</c> marks a <b>read model</b> property.
/// They are usually different names — <c>key documentId</c> with <c>id = documentId</c> keys the model's
/// <c>Id</c> — so the key is matched by what a property is mapped <i>from</i> first, and only then by name.
/// <para>
/// <c>[Key]</c> is identity, not routing. Chronicle never reads it to decide which document an event updates —
/// it seeds every <c>From</c> with the event source id and only overwrites that from a class-level
/// <c>[FromEvent]</c>'s key, which <see cref="StateViewSliceRenderer"/> renders. A key written on a <c>from</c>
/// block or on one of its events therefore has to reach <c>[FromEvent]</c> to have any effect; a key written on
/// the projection itself drives neither, which matches the kernel's own visitor — it never reads
/// <see cref="ProjectionSyntax.Key"/> — so both route on the event source id and agree.
/// </para>
/// </remarks>
public static class ProjectionKey
{
    /// <summary>
    /// Resolves the read-model property to mark with <c>[Key]</c>, adding a property for the key when the
    /// projection maps nothing from it.
    /// </summary>
    /// <param name="projection">The <see cref="ProjectionSyntax"/> being rendered.</param>
    /// <param name="fromBlocks">The projection's <c>from</c> blocks.</param>
    /// <param name="properties">The inferred read-model properties; a synthesized key property is appended here.</param>
    /// <param name="events">The <see cref="EventPropertyIndex"/> to type a synthesized key property against.</param>
    /// <param name="applicationSet">The <see cref="ApplicationSet"/> to resolve concept types against.</param>
    /// <param name="diagnostics">Collects anything that could not be rendered faithfully.</param>
    /// <returns>The name of the key property, or <see langword="null"/> when the projection declares no usable key.</returns>
    public static string? Resolve(
        ProjectionSyntax projection,
        IReadOnlyList<FromSyntax> fromBlocks,
        ICollection<MappedProperty> properties,
        EventPropertyIndex events,
        ApplicationSet applicationSet,
        ICollection<string> diagnostics)
    {
        var declaring = fromBlocks.FirstOrDefault(from => from.Key is not null);
        var key = projection.Key ?? declaring?.Key;

        ReportInlineKeys(fromBlocks, key, diagnostics);

        if (key is CompositeKeySyntax composite)
        {
            diagnostics.Add($"Composite key '{composite.Type}' has no model-bound equivalent — the read model is rendered without a key.");
            return null;
        }

        if (key is not ExpressionKeySyntax { Expression: PathExpressionSyntax path })
        {
            return null;
        }

        var mappedFromKey = properties.FirstOrDefault(property =>
            property.SourcePath is not null && string.Equals(property.SourcePath, path.Path, StringComparison.OrdinalIgnoreCase));
        if (mappedFromKey is not null)
        {
            return mappedFromKey.Name;
        }

        var keyPropertyName = Identifiers.ToPascalCase(path.Path);
        var named = properties.FirstOrDefault(property => property.Name == keyPropertyName);
        if (named is not null)
        {
            return named.Name;
        }

        properties.Add(Synthesize(keyPropertyName, path.Path, declaring, events, applicationSet, diagnostics));
        return keyPropertyName;
    }

    // A key written on one event of a 'from' routes that event on its own and wins over the block's key, which is
    // rendered onto that event's [FromEvent]. The read model itself is identified by one property, so where an
    // inline key names something the resolved key does not, the two describe the same document differently and
    // the difference is named rather than left for whoever reads the generated file to notice.
    static void ReportInlineKeys(IReadOnlyList<FromSyntax> fromBlocks, KeySyntax? resolved, ICollection<string> diagnostics)
    {
        var resolvedPath = resolved is ExpressionKeySyntax { Expression: PathExpressionSyntax path } ? path.Path : null;
        var differing = fromBlocks
            .SelectMany(from => from.Events)
            .Where(spec => spec.Key is PathExpressionSyntax inline && !string.Equals(inline.Path, resolvedPath, StringComparison.OrdinalIgnoreCase))
            .Select(spec => spec.Event)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (differing.Length == 0)
        {
            return;
        }

        var named = string.Join(", ", differing.Select(name => $"'{name}'"));
        diagnostics.Add(
            resolvedPath is null
                ? $"{named} declare(s) a key inside a 'from' block and nothing else declares one — those events route on it, but the read model is rendered without a key property."
                : $"{named} declare(s) a key of its own inside a 'from' block — those events route on it, while the read model is identified by '{resolvedPath}'.");
    }

    static MappedProperty Synthesize(
        string keyPropertyName,
        string keyPath,
        FromSyntax? declaring,
        EventPropertyIndex events,
        ApplicationSet applicationSet,
        ICollection<string> diagnostics)
    {
        var eventName = declaring?.Events.FirstOrDefault()?.Event;
        var typeRef = eventName is null ? null : events.TypeOf(eventName, keyPath);

        if (typeRef is null)
        {
            diagnostics.Add($"Key '{keyPath}' is mapped to no read model property and its type could not be resolved — rendered as 'object'.");
            return new MappedProperty(keyPropertyName, new ResolvedType("object", false, false, ResolvedTypeKind.Unresolved, keyPath), null, keyPath);
        }

        diagnostics.Add($"Key '{keyPath}' is mapped to no read model property — a '{keyPropertyName}' property was added to carry it.");
        return new MappedProperty(keyPropertyName, TypeResolver.Resolve(typeRef, applicationSet), null, keyPath);
    }
}
