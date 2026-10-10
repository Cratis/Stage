// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics.Policies;

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
        if (!EsmSchemaV7Support.Supports(model.LanguageVersion, model.SemanticVersion))
        {
            diagnostics.Add(Error("STAGE-ESM-016", "The model's language/semantic version is not one the Cratis ESM planner has audited.", model.Application.Id));
            return [.. diagnostics];
        }

        diagnostics.AddRange(SemanticVersionFeatureAdmission.Verify(context, slices));
        if (diagnostics.Count > 0)
        {
            return [.. diagnostics];
        }

        ValidateEventRevisions(context, slices, diagnostics);
        if (diagnostics.Count > 0)
        {
            return [.. diagnostics];
        }

        // TypedContexts is reserved for reducer and policy runtime tokens and wrappers. A modeled
        // namespace with the same root path can rebind TenantId or Identity in unrelated artifacts.
        if ((slices.Any(_ => !_.Slice.Reducers.IsEmpty) || UsesOpaquePolicies(context, slices)) &&
            context.NamespacePaths.Any(path => path == $"{context.RootNamespace}.TypedContexts"))
        {
            diagnostics.Add(Error("STAGE-ESM-022", "Generated namespace 'TypedContexts' shadows the reserved reducer and policy runtime namespace.", model.Application.Id));
            return [.. diagnostics];
        }

        ValidateEventSourceRoutes(context, slices, diagnostics);
        if (diagnostics.Count > 0)
        {
            return [.. diagnostics];
        }

        ValidateStrings(context, slices, diagnostics);
        ValidateTypes(context, diagnostics);
        ValidateConstraints(context, slices, diagnostics);
        foreach (var (artifact, kind, name) in GeneratedTypeNames.Collisions(
            context.Application,
            slices.Select(located => ((IEnumerable<string>)located.Path, located.Slice)),
            SelectedConstraints(context, slices).Select(selected =>
                ((IEnumerable<string>)context.Slice(selected.Slice.Id).Path, selected.Slice.Id, selected.Constraint)),
            rendersStringsCatalog: context.Strings is not null,
            opaquePolicyTypes: SemanticPolicyContextRuntime.GeneratedTypes(context, slices),
            commonNamespace: string.Join('.', context.Domain.Append("Common"))))
        {
            var message = kind == "Namespace"
                ? $"Generated type '{name}' collides with a generated C# namespace."
                : $"{kind} '{name}' collides with another generated C# type in the same namespace.";
            diagnostics.Add(Error("STAGE-ESM-012", message, artifact));
        }

        var analysedBodies = 0;
        foreach (var located in slices)
        {
            foreach (var @event in located.Slice.Events.Where(@event =>
                !GeneratedPascalCase.EventMembersAreUnique(@event.Name, @event.Properties.Select(property => property.Name))))
            {
                diagnostics.Add(Error("STAGE-ESM-012", $"Event '{@event.Name}' has property names that collide in generated C#.", @event.Id));
            }

            analysedBodies += ValidateReducers(context, located.Slice, diagnostics);

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

            ValidateCallerFixtures(located.Slice, diagnostics);
            SemanticSpecificationAdmission.Validate(context, located.Slice, diagnostics);
        }

        if (slices.Any(_ => !_.Slice.Reducers.IsEmpty))
        {
            diagnostics.Add(new(
                "STAGE-ESM-023",
                ArtifactRenderDiagnosticSeverity.Information,
                $"{analysedBodies} transition bodies analysed.",
                model.Application.Id));
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

    internal static bool UsesOpaquePolicies(SemanticApplicationContext context, IEnumerable<LocatedSemanticSlice> slices) =>
        slices.SelectMany(located => located.Slice.Commands.Select(command => command.Authorization)
                .Concat(located.Slice.Queries.Select(query => query.Authorization)))
            .OfType<SemanticAuthorization>()
            .Any(authorization => OpaquePolicies(authorization, context.Application.Policies).Any());

    static void ValidateEventSourceRoutes(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices, List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (!context.Application.EventSources.IsEmpty)
        {
            diagnostics.Add(Error("STAGE-ESM-030", "Named event sources and streams are not yet supported by the Cratis ESM planner.", context.Application.Id));
        }

        foreach (var command in slices.SelectMany(_ => _.Slice.Commands).Where(_ => _.Route is not null))
        {
            diagnostics.Add(Error("STAGE-ESM-030", $"Command '{command.Name}' has an event-source route, which the Cratis ESM planner cannot render yet.", command.Id));
        }

        foreach (var specification in slices.SelectMany(_ => _.Slice.Specifications))
        {
            if (specification.WhenAppended?.Route is not null || specification.ThenEvents.Any(_ => _.Route is not null || _.Unrouted))
            {
                diagnostics.Add(Error("STAGE-ESM-030", $"Specification '{specification.Name}' uses event-source routing assertions, which the Cratis ESM planner cannot render yet.", specification.Id));
            }
        }
    }

    static bool IsProperty(SemanticExpression? expression, SemanticExpressionRootKind root, IEnumerable<SemanticId> candidates) =>
        expression is SemanticResolvedExpression { Source: SemanticExpressionSourceKind.Property } resolved &&
        resolved.Root == root && candidates.Contains(resolved.Target);

    static ArtifactRenderDiagnostic Error(string code, string message, SemanticId artifact) =>
        new(code, ArtifactRenderDiagnosticSeverity.Error, message, artifact);
}
