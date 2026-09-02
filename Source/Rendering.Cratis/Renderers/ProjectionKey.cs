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

        return ResolveIdentifying(path.Path, $"Key '{path.Path}'", "read model", declaring, properties, events, applicationSet, diagnostics);
    }

    /// <summary>
    /// Resolves the property a key path identifies on a record, adding one to carry it when nothing maps from it.
    /// </summary>
    /// <param name="keyPath">The Screenplay path the key is written as.</param>
    /// <param name="subject">How the key is named in a diagnostic, e.g. <c>Key 'invoiceNumber'</c>.</param>
    /// <param name="target">What the record is called in a diagnostic, e.g. <c>read model</c>.</param>
    /// <param name="declaring">The <c>from</c> block to type a synthesized property against, when there is one.</param>
    /// <param name="properties">The inferred properties; a synthesized key property is appended here.</param>
    /// <param name="events">The <see cref="EventPropertyIndex"/> to type a synthesized key property against.</param>
    /// <param name="applicationSet">The <see cref="ApplicationSet"/> to resolve concept types against.</param>
    /// <param name="diagnostics">Collects anything that could not be rendered faithfully.</param>
    /// <returns>The name of the identifying property.</returns>
    /// <remarks>
    /// A key names an <b>event</b> property while the property it identifies is a <b>record</b> property, so it is
    /// matched by what a property is mapped <i>from</i> first and only then by name. Both the read model's
    /// <c>key</c> and a <c>children</c> block's <c>identified by</c> resolve this way, and both need the property
    /// to exist — a name that is on no record identifies nothing.
    /// </remarks>
    public static string ResolveIdentifying(
        string keyPath,
        string subject,
        string target,
        FromSyntax? declaring,
        ICollection<MappedProperty> properties,
        EventPropertyIndex events,
        ApplicationSet applicationSet,
        ICollection<string> diagnostics)
    {
        var mappedFromKey = properties.FirstOrDefault(property =>
            property.SourcePath is not null && string.Equals(property.SourcePath, keyPath, StringComparison.OrdinalIgnoreCase));
        if (mappedFromKey is not null)
        {
            return mappedFromKey.Name;
        }

        var keyPropertyName = Identifiers.ToPascalCase(keyPath);
        var named = properties.FirstOrDefault(property => property.Name == keyPropertyName);
        if (named is not null)
        {
            return named.Name;
        }

        properties.Add(Synthesize(keyPropertyName, keyPath, subject, target, declaring, events, applicationSet, diagnostics));
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
        string subject,
        string target,
        FromSyntax? declaring,
        EventPropertyIndex events,
        ApplicationSet applicationSet,
        ICollection<string> diagnostics)
    {
        var eventName = declaring?.Events.FirstOrDefault()?.Event;
        var typeRef = eventName is null ? null : events.TypeOf(eventName, keyPath);

        if (typeRef is null)
        {
            diagnostics.Add($"{subject} is mapped to no {target} property and its type could not be resolved — rendered as 'object'.");
            return new MappedProperty(keyPropertyName, new ResolvedType("object", false, false, ResolvedTypeKind.Unresolved, keyPath), null, keyPath);
        }

        diagnostics.Add($"{subject} is mapped to no {target} property — a '{keyPropertyName}' property was added to carry it.");
        return new MappedProperty(keyPropertyName, TypeResolver.Resolve(typeRef, applicationSet), null, keyPath);
    }
}
