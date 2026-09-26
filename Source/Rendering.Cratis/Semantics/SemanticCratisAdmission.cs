// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Admits only the ESM subset the first direct Cratis planner renders exactly.
/// </summary>
internal static partial class SemanticCratisAdmission
{
    /// <summary>
    /// Evaluates target support for the selected semantic scope.
    /// </summary>
    /// <param name="context">The indexed semantic application.</param>
    /// <param name="slices">The selected slices.</param>
    /// <returns>Blocking diagnostics for unsupported semantics.</returns>
    public static ImmutableArray<ArtifactRenderDiagnostic> Evaluate(
        SemanticApplicationContext context,
        IReadOnlyList<LocatedSemanticSlice> slices)
    {
        // Keep the audited surface available in Release as well as in the Debug-only specifications.
        _ = SemanticSurfaceLedger.Entries;
        var diagnostics = new List<ArtifactRenderDiagnostic>();
        var model = context.Request.Model;
        if (!EsmSchemaV3Support.Supports(model.LanguageVersion, model.SemanticVersion))
        {
            diagnostics.Add(Error("STAGE-ESM-016", "The model's language/semantic version is not one the Cratis ESM planner has audited.", model.Application.Id));
            return [.. diagnostics];
        }

        ValidateStrings(context, slices, diagnostics);
        ValidateTypes(context, diagnostics);
        ValidateConstraints(context, slices, diagnostics);

        foreach (var located in slices)
        {
            if (!located.Slice.Reducers.IsEmpty)
            {
                diagnostics.Add(Error("STAGE-ESM-019", $"Slice '{located.Slice.Name}' has reducer implementation bodies; typed contexts are available, but Stage#119 still needs to render reducer bodies with null-result deletion, event-source key semantics and pure capability enforcement.", located.Slice.Id));
                continue;
            }

            switch (located.Slice.Kind)
            {
                case SemanticSliceKind.StateChange:
                    ValidateStateChange(context, located.Slice, diagnostics);
                    break;
                case SemanticSliceKind.StateView:
                    ValidateStateView(context, located.Slice, diagnostics);
                    break;
                default:
                    diagnostics.Add(Error("STAGE-ESM-001", "The slice kind is not supported by the Cratis ESM planner.", located.Slice.Id));
                    break;
            }

            SemanticSpecificationAdmission.Validate(context, located.Slice, diagnostics);
        }

        return [.. diagnostics];
    }

    /// <summary>
    /// Checks referenced localized messages before generating any artifacts.
    /// </summary>
    /// <param name="context">The semantic application.</param>
    /// <param name="slices">The selected slices.</param>
    /// <param name="diagnostics">The blocking diagnostics.</param>
    internal static void ValidateStrings(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices, List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (context.Strings is null)
        {
            return;
        }

        var messages = context.Application.Concepts.SelectMany(concept => concept.Validations.Select(rule => (rule.Message, concept.Id)))
            .Concat(slices.SelectMany(located => located.Slice.Commands.SelectMany(command =>
                command.Validations.Select(rule => (rule.Message, command.Id))
                    .Concat(command.Requirements.Select(requirement => (requirement.Message, command.Id))))));
        foreach (var (message, artifact) in messages.Where(_ => _.Message?.StartsWith("$strings.", StringComparison.Ordinal) == true))
        {
            var key = message!["$strings.".Length..];
            if (!context.Strings.Locales[context.Strings.DefaultLocale].ContainsKey(key))
            {
                diagnostics.Add(Error("STAGE-ESM-018", $"String key '{message}' is missing from the default locale '{context.Strings.DefaultLocale}'.", artifact));
                continue;
            }

            if (context.Strings.Locales.Values.Any(locale => locale.TryGetValue(key, out var value) &&
                value.Any(character => character is '{' or '}' || char.IsControl(character) || char.IsSurrogate(character))))
            {
                diagnostics.Add(Error("STAGE-ESM-018", $"String key '{message}' contains formatting or control characters the Cratis validator cannot preserve.", artifact));
            }
        }
    }

    /// <summary>
    /// Checks whether a semantic type refers to a supported, resolved type.
    /// </summary>
    /// <param name="context">The indexed semantic application.</param>
    /// <param name="type">The declared type.</param>
    /// <returns>Whether the declared type exists.</returns>
    /// <exception cref="UnsupportedSemanticRendering">The type reference kind is not handled.</exception>
    internal static bool TypeExists(SemanticApplicationContext context, SemanticTypeReference type) => type.Kind switch
    {
        SemanticTypeReferenceKind.Primitive => type.Primitive != SemanticPrimitiveType.Unknown,
        SemanticTypeReferenceKind.Concept => context.Concepts.ContainsKey(type.Target),
        SemanticTypeReferenceKind.CompositeType => context.Types.ContainsKey(type.Target),
        SemanticTypeReferenceKind.Unknown => false,
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticTypeReferenceKind), type.Kind)
    };

    static bool MappingsMatch(
        ImmutableArray<SemanticPropertyMapping> mappings,
        ImmutableArray<SemanticProperty> targets,
        ImmutableArray<SemanticProperty> sources,
        SemanticExpressionRootKind root) =>
        mappings.Length == targets.Length && targets.All(target => mappings.Any(mapping => mapping.TargetProperty == target.Id &&
            IsProperty(mapping.Source, root, sources.Select(_ => _.Id))));

    static bool IsProperty(SemanticExpression? expression, SemanticExpressionRootKind root, IEnumerable<SemanticId> candidates) =>
        expression is SemanticResolvedExpression { Source: SemanticExpressionSourceKind.Property } resolved &&
        resolved.Root == root && candidates.Contains(resolved.Target);

    static ArtifactRenderDiagnostic Error(string code, string message, SemanticId artifact) =>
        new(code, ArtifactRenderDiagnosticSeverity.Error, message, artifact);
}
