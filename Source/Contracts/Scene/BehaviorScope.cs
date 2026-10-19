// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneModel = Cratis.Scene.Model.Interactions;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Carries what a <c language="csharp">uses</c> clause resolves against, and where to report one that resolves to nothing.
/// </summary>
/// <param name="Declared">The behaviors the document declares, by name.</param>
/// <param name="Findings">Where to report an unresolved <c language="csharp">uses</c>.</param>
public sealed record BehaviorScope(
    IReadOnlyDictionary<string, ScreenplaySyntax.BehaviorSyntax> Declared,
    ICollection<RenderFinding> Findings)
{
    /// <summary>
    /// Gets the collector for omitted interaction constructs, or null for legacy throwing behavior.
    /// </summary>
    public ConversionDiagnostics? Diagnostics { get; init; }

    /// <summary>
    /// Gets the members of every enumeration-typed read model field, by read model and then field.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>>? Enumerations { get; init; }

    /// <summary>
    /// Gets the read model the nearest <c language="csharp">data</c> declaration reads, which a guard's <c language="csharp">item</c> is.
    /// </summary>
    public string? Subject { get; init; }

    /// <summary>
    /// Gets the modules, features and slices of the application, which a toolbar button's slice reference resolves against.
    /// </summary>
    public Cratis.Scene.Model.Structure.ApplicationStructure? Structure { get; init; }

    /// <summary>
    /// A scope for a document that declares nothing, used where behaviors are not being translated.
    /// </summary>
    public static BehaviorScope None => new(new Dictionary<string, ScreenplaySyntax.BehaviorSyntax>(StringComparer.Ordinal), []);

    /// <summary>
    /// Creates a scope from a document's declarations.
    /// </summary>
    /// <param name="syntax">The application to read declarations from.</param>
    /// <param name="findings">Where to report an unresolved <c language="csharp">uses</c>.</param>
    /// <returns>The <see cref="BehaviorScope"/>.</returns>
    public static BehaviorScope For(ScreenplaySyntax.ApplicationSyntax syntax, ICollection<RenderFinding> findings) =>
        new(BehaviorAttachments.Declared(syntax.Behaviors), findings) { Enumerations = EnumerationsIn(syntax), Structure = StructureConverter.Convert(syntax) };

    /// <summary>
    /// Gets the members of the enumeration a guard's subject field has, in declared order, which is the order the
    /// generated backend numbers them in.
    /// </summary>
    /// <param name="path">The guard field, such as <c language="csharp">item.status</c>.</param>
    /// <returns>The members, or null when the field is not an enumeration or the subject is not known.</returns>
    public IReadOnlyList<string>? EnumerationOf(string path)
    {
        if (Subject is null || Enumerations is null || !path.StartsWith(SceneGuards.SubjectPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        var field = path[SceneGuards.SubjectPrefix.Length..];
        if (Enumerations.TryGetValue(Subject, out var fields) && fields.TryGetValue(field, out var members))
        {
            return members;
        }

        return null;
    }

    /// <summary>
    /// Resolves what is attached at one point.
    /// </summary>
    /// <param name="inline">The inline <c language="csharp">on</c> blocks written there.</param>
    /// <param name="used">The <c language="csharp">uses</c> clauses written there.</param>
    /// <param name="subject">What is being attached to, for reporting.</param>
    /// <returns>The attached behaviors, in authored order.</returns>
    public IReadOnlyList<SceneModel.Behavior> Resolve(
        IEnumerable<ScreenplaySyntax.BehaviorSyntax> inline,
        IEnumerable<ScreenplaySyntax.UsesBehaviorSyntax> used,
        string subject) =>
        BehaviorAttachments.Convert(inline, used, Declared, subject, Findings, EnumerationOf, Diagnostics);

    static Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>> EnumerationsIn(ScreenplaySyntax.ApplicationSyntax syntax)
    {
        var enumerations = syntax.Concepts
            .Where(concept => concept.IsEnum)
            .GroupBy(concept => concept.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<string>)[.. group.First().Values], StringComparer.Ordinal);
        var result = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>>(StringComparer.Ordinal);

        foreach (var readModel in syntax.Modules.SelectMany(module => Slices(module.Features)).SelectMany(slice => slice.ReadModels ?? []))
        {
            result[readModel.Name] = readModel.Properties
                .Where(property => enumerations.ContainsKey(property.Type.Name))
                .ToDictionary(property => property.Name, property => enumerations[property.Type.Name], StringComparer.OrdinalIgnoreCase);
        }

        return result;
    }

    static IEnumerable<ScreenplaySyntax.SliceSyntax> Slices(IEnumerable<ScreenplaySyntax.FeatureSyntax> features) =>
        features.SelectMany(feature => feature.Slices.Concat(Slices(feature.Features)));
}
