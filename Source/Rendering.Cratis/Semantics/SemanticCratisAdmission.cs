// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics.Policies;
using Cratis.Stage.Rendering.Cratis.Semantics.Validation;

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
        var diagnostics = new List<ArtifactRenderDiagnostic>();

        // Standalone admission callers still need the version gates; the planner has already run them.
        if (!SemanticVersionFeatures.Supports(context.Request.Model))
        {
            diagnostics.Add(Error("STAGE-ESM-016", "The model's language/semantic version is not one the Cratis ESM planner has audited.", context.Application.Id));
            return [.. diagnostics];
        }

        diagnostics.AddRange(SemanticVersionFeatureAdmission.Verify(context, slices));
        Evaluate(context, slices, diagnostics);
        return [.. diagnostics];
    }

    internal static void Evaluate(
        SemanticApplicationContext context,
        IReadOnlyList<LocatedSemanticSlice> slices,
        List<ArtifactRenderDiagnostic> diagnostics)
    {
        // Keep the audited surface available in Release as well as in the Debug-only specifications.
        _ = SemanticSurfaceLedger.Entries;
        PassesPreconditions(context, slices, diagnostics);

        Collect(() => ValidateStrings(context, slices, diagnostics), context.Application.Id, diagnostics);
        Collect(() => ValidateMatchPatterns(context, slices, diagnostics), context.Application.Id, diagnostics);
        Collect(() => ValidateTypes(context, diagnostics), context.Application.Id, diagnostics);
        Collect(() => ValidateCompliance(context, diagnostics), context.Application.Id, diagnostics);
        Collect(() => ValidateConstraints(context, slices, diagnostics), context.Application.Id, diagnostics);
        Collect(() => ValidateGeneratedTypeNames(context, slices, diagnostics), context.Application.Id, diagnostics);
        Collect(() => ValidateEventTypeIdentities(context, slices, diagnostics), context.Application.Id, diagnostics);

        var analysedBodies = 0;
        foreach (var located in slices)
        {
            Collect(() => analysedBodies += ValidateSlice(context, located, diagnostics), located.Slice.Id, diagnostics);
        }

        if (slices.Any(_ => !_.Slice.Reducers.IsEmpty))
        {
            diagnostics.Add(new(
                "STAGE-ESM-023",
                ArtifactRenderDiagnosticSeverity.Information,
                $"{analysedBodies} transition bodies analysed.",
                context.Request.Model.Application.Id));
        }
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
            ValidateStringKey(context.Strings, message!, artifact, diagnostics);
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

    /// <summary>
    /// Enumerates every top-level type the rendered application declares for the selected slices, with namespaces relative
    /// to the root namespace — the inventory admission checks for collisions and the C# import resolver resolves against.
    /// </summary>
    /// <param name="context">The application being rendered.</param>
    /// <param name="slices">The selected slices.</param>
    /// <returns>The generated declarations.</returns>
    internal static IEnumerable<GeneratedTypeDeclaration> GeneratedDeclarations(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices) =>
        GeneratedTypeNames.Declarations(
            context.Application,
            slices.Select(located => ((IEnumerable<string>)located.Path, located.Slice)),
            SelectedConstraints(context, slices).Select(selected =>
                ((IEnumerable<string>)context.Slice(selected.Slice.Id).Path, selected.Slice.Id, selected.Constraint)),
            rendersStringsCatalog: context.Strings is not null,
            opaquePolicyTypes: SemanticPolicyContextRuntime.GeneratedTypes(context, slices).Concat(ValidationTypes(context, slices)),
            commonNamespace: string.Join('.', context.Domain.Append("Common")),
            eventSources: SemanticEventSourceArtifactRenderer.Selected(context, slices),
            eventSourcesNamespace: string.Join('.', context.Domain.Append("EventSources")),
            policiesNamespace: string.Join('.', context.Domain.Append("GeneratedPolicies")),
            selfReducing: context.SelfReducing);

    static IEnumerable<(string Namespace, string Name)> ValidationTypes(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices)
    {
        var descriptors = SemanticValidationContextRuntime.Selected(context, slices).ToArray();
        if (descriptors.Length == 0) yield break;
        var ns = string.Join('.', context.Domain.Append("TypedContexts"));
        yield return (ns, "ValidationBodies");
        foreach (var descriptor in descriptors) yield return (ns, SemanticValidationContextRuntime.Wrapper(descriptor));
    }

    static void PassesPreconditions(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices, List<ArtifactRenderDiagnostic> diagnostics)
    {
        // These checks refuse target capabilities, not the model's indexed structure. Current event
        // shapes remain available after a migration refusal, so independent body/spec checks can run.
        Collect(() => diagnostics.AddRange(SemanticDateComparisonAdmission.Verify(context, slices)), context.Application.Id, diagnostics);
        Collect(() => ValidateEventRevisions(context, slices, diagnostics), context.Application.Id, diagnostics);
        Collect(() => ValidateReservedNamespace(context, slices, diagnostics), context.Application.Id, diagnostics);
        Collect(() => ValidateEventSourceRoutes(context, slices, diagnostics), context.Application.Id, diagnostics);
        Collect(() => ValidateWholeNumberLiterals(context, slices, diagnostics), context.Application.Id, diagnostics);
    }

    static void Collect(Action validate, SemanticId artifact, List<ArtifactRenderDiagnostic> diagnostics)
    {
        try
        {
            validate();
        }
        catch (UnsupportedSemanticRendering exception)
        {
            diagnostics.Add(Error(exception.Code, exception.Message, artifact));
        }
        catch (InvalidTypedContext exception)
        {
            diagnostics.Add(Error("STAGE-ESM-021", exception.Message, artifact));
        }
    }

    static void ValidateReservedNamespace(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices, List<ArtifactRenderDiagnostic> diagnostics)
    {
        // TypedContexts is reserved for reducer and policy runtime tokens and wrappers. A modeled
        // namespace with the same root path can rebind TenantId or Identity in unrelated artifacts.
        if ((slices.Any(_ => !_.Slice.Reducers.IsEmpty) || UsesOpaquePolicies(context, slices) || SemanticValidationContextRuntime.Selected(context, slices).Any()) && DeclaresTypedContexts(context))
        {
            diagnostics.Add(Error("STAGE-ESM-022", "Generated namespace 'TypedContexts' shadows the reserved reducer and policy runtime namespace.", context.Request.Model.Application.Id));
        }
    }

    static bool DeclaresTypedContexts(SemanticApplicationContext context) =>
        context.NamespacePaths.Any(path => path == $"{context.RootNamespace}.TypedContexts" || path == context.TypedContextsNamespace);

    static void ValidateGeneratedTypeNames(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices, List<ArtifactRenderDiagnostic> diagnostics)
    {
        foreach (var (artifact, kind, name) in GeneratedTypeNames.Collisions(slices.Select(located => (IEnumerable<string>)located.Path), GeneratedDeclarations(context, slices)))
        {
            var message = kind == "Namespace"
                ? $"Generated type '{name}' collides with a generated C# namespace."
                : $"{kind} '{name}' collides with another generated C# type in the same namespace.";
            diagnostics.Add(Error("STAGE-ESM-012", message, artifact));
        }
    }

    static int ValidateSlice(SemanticApplicationContext context, LocatedSemanticSlice located, List<ArtifactRenderDiagnostic> diagnostics)
    {
        foreach (var @event in located.Slice.Events.Where(@event =>
            !GeneratedPascalCase.EventMembersAreUnique(@event.Name, @event.Properties.Select(property => property.Name))))
        {
            diagnostics.Add(Error("STAGE-ESM-012", $"Event '{@event.Name}' has property names that collide in generated C#.", @event.Id));
        }

        var analysedBodies = 0;
        Collect(() => analysedBodies = ValidateReducers(context, located.Slice, diagnostics), located.Slice.Id, diagnostics);
        Collect(() => ValidateSliceKind(context, located.Slice, diagnostics), located.Slice.Id, diagnostics);
        Collect(() => ValidateCallerFixtures(located.Slice, diagnostics), located.Slice.Id, diagnostics);
        Collect(() => SemanticSpecificationAdmission.Validate(context, located.Slice, diagnostics), located.Slice.Id, diagnostics);

        return analysedBodies;
    }

    static void ValidateSliceKind(SemanticApplicationContext context, SemanticSlice slice, List<ArtifactRenderDiagnostic> diagnostics)
    {
        switch (slice.Kind)
        {
            case SemanticSliceKind.StateChange:
                ValidateStateChange(context, slice, diagnostics);
                break;
            case SemanticSliceKind.Automation:
                ValidateAutomation(context, slice, diagnostics);
                break;
            case SemanticSliceKind.StateView:
                ValidateStateView(context, slice, diagnostics);
                break;
            default:
                diagnostics.Add(Error("STAGE-ESM-001", "The slice kind is not supported by the Cratis ESM planner.", slice.Id));
                break;
        }
    }

    static void ValidateStringKey(StringsCatalogInput.Catalog strings, string message, SemanticId artifact, List<ArtifactRenderDiagnostic> diagnostics)
    {
        var key = message["$strings.".Length..];
        if (!strings.Locales[strings.DefaultLocale].ContainsKey(key))
        {
            diagnostics.Add(Error("STAGE-ESM-018", $"String key '{message}' is missing from the default locale '{strings.DefaultLocale}'.", artifact));
            return;
        }

        if (strings.Locales.Values.Any(locale => locale.TryGetValue(key, out var value) &&
            value.Any(IsUnpreservable)))
        {
            diagnostics.Add(Error("STAGE-ESM-018", $"String key '{message}' contains formatting or control characters the Cratis validator cannot preserve.", artifact));
        }
    }

    static bool IsUnpreservable(char character) => character is '{' or '}' || char.IsControl(character) || char.IsSurrogate(character);

    static void ValidateEventSourceRoutes(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices, List<ArtifactRenderDiagnostic> diagnostics)
    {
        foreach (var source in SemanticEventSourceArtifactRenderer.Selected(context, slices))
        {
            ValidateReservedNames(source, diagnostics);
            ValidateEventSourceNames(source, diagnostics);
        }

        foreach (var specification in slices.SelectMany(_ => _.Slice.Specifications))
        {
            ValidateFixtureRoutes(context, specification, diagnostics);
        }
    }

    static void ValidateReservedNames(SemanticEventSource source, List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (source.SourceKind == "Default")
        {
            diagnostics.Add(Error("STAGE-ESM-030", "The stored event source name 'Default' is reserved by Chronicle.", source.Id));
        }

        foreach (var stream in source.Streams.Where(stream => stream.StreamKind == "All"))
        {
            diagnostics.Add(Error("STAGE-ESM-030", "The stored event stream name 'All' is reserved by Chronicle.", stream.Id));
        }
    }

    static void ValidateFixtureRoutes(SemanticApplicationContext context, SemanticSpecification specification, List<ArtifactRenderDiagnostic> diagnostics)
    {
        var fixtures = specification.GivenEvents.Concat(specification.ThenEvents).Where(occurrence => occurrence.Route is not null).ToArray();
        if (fixtures.Any(occurrence => !SemanticSpecificationRouteRendering.CanFormat(occurrence.Route!, context)))
        {
            diagnostics.Add(Error("STAGE-ESM-030", $"Specification '{specification.Name}' has a fixture route whose stream identity cannot be formatted portably.", specification.Id));
        }

        // Companion scenarios replay actual command routes, even if a then-event leaves its route unspecified.
        if ((!specification.ThenReadModels.IsEmpty || !specification.ThenAbsentReadModels.IsEmpty || !specification.ThenQueries.IsEmpty) &&
            SemanticSpecificationRouteRendering.ForCommand(specification, context) is { } commandRoute &&
            !SemanticSpecificationRouteRendering.CanFormat(commandRoute, context))
        {
            diagnostics.Add(Error("STAGE-ESM-030", $"Specification '{specification.Name}' has a command replay route whose stream identity cannot be formatted portably.", specification.Id));
        }
    }

    static bool IsProperty(SemanticExpression? expression, SemanticExpressionRootKind root, IEnumerable<SemanticId> candidates) =>
        expression is SemanticResolvedExpression { Source: SemanticExpressionSourceKind.Property } resolved &&
        resolved.Root == root && candidates.Contains(resolved.Target);

    static ArtifactRenderDiagnostic Error(string code, string message, SemanticId artifact) =>
        new(code, ArtifactRenderDiagnosticSeverity.Error, message, artifact);
}
