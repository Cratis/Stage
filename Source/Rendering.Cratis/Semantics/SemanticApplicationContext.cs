// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Cratis.Stage.Rendering.Cratis.Semantics.Projections;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Indexes one executable semantic application by stable identity for direct Cratis rendering.
/// </summary>
internal sealed class SemanticApplicationContext
{
    readonly Dictionary<SemanticId, LocatedSemanticSlice> _slices = [];
    readonly Dictionary<SemanticId, SemanticCaptureMemberPlan> _captureMembers = [];
    readonly Lazy<IReadOnlyList<GeneratedTypeName>> _generatedTypeSeed;
    readonly Lazy<IReadOnlyList<GeneratedTypeName>> _generatedTypes;
    readonly Lazy<IReadOnlyDictionary<SemanticId, string>> _responseTypes;
    readonly Dictionary<(SemanticSlice Slice, SemanticId? Reducer), SemanticReducerBindingEnvironment> _reducerBindings = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticApplicationContext"/> class.
    /// </summary>
    /// <param name="request">The immutable artifact render request.</param>
    /// <param name="options">The explicit options admitted by the complete package-profile validation.</param>
    public SemanticApplicationContext(ArtifactRenderRequest request, CratisRenderingOptions options)
    {
        Request = request;
        var performed = SemanticPerformedQueries.Lift(request);
        Application = performed.Application;
        PerformedQueries = performed.Performed;
        PerformedQueryRejections = performed.Rejections;
        RootNamespace = options.RootNamespace;
        Domain = DomainPlacementInput.From(request.Profile);
        Strings = StringsCatalogInput.From(request.Profile);
        ReducerConventionInput.TryRead(request.Profile, Application.Name, out var reducerConvention);
        ReducerConvention = reducerConvention;
        var metadata = request.Profile.Inputs.SingleOrDefault(input => input.Name == AuthoringMetadataInput.Name);
        Documentation = metadata is not null && AuthoringMetadataInput.TryRead(metadata, request.Model, out var catalog) ? catalog : null;
        Concepts = IndexIdentities(Application.Concepts, _ => _.Id, "concept");
        Types = IndexIdentities(Application.Types, _ => _.Id, "composite type");

        foreach (var module in Application.Modules)
        {
            foreach (var feature in module.Features)
            {
                IndexFeature(module, feature, [.. Domain, module.Name]);
            }
        }

        Events = IndexIdentities(_slices.Values.SelectMany(_ => _.Slice.Events), _ => _.Id, "event");
        Commands = IndexIdentities(_slices.Values.SelectMany(_ => _.Slice.Commands), _ => _.Id, "command");
        ReadModels = IndexIdentities(_slices.Values.SelectMany(_ => _.Slice.ReadModels), _ => _.Id, "read model");
        Projections = IndexIdentities(_slices.Values.SelectMany(_ => _.Slice.Projections), _ => _.Id, "projection");
        Reducers = [.. _slices.Values.SelectMany(_ => _.Slice.Reducers)];
        Queries = IndexIdentities(_slices.Values.SelectMany(_ => _.Slice.Queries), _ => _.Id, "query");
        MemberNames = new(Commands.Values, ReadModels.Values, Queries.Values);
        Specifications = IndexIdentities(_slices.Values.SelectMany(_ => _.Slice.Specifications), _ => _.Id, "specification");
        IdentifierConcepts = FindIdentifierConcepts();
        Constraints = [.. _slices.Values.SelectMany(located => located.Slice.Constraints.Select(constraint => (located.Slice, constraint)))];
        _generatedTypeSeed = new(() =>
        [
            new(string.Empty, "Program"),
            .. SemanticCratisAdmission.GeneratedDeclarations(this, [.. _slices.Values]).Select(type =>
                new GeneratedTypeName(CatalogNames.Qualify(RootNamespace, type.Namespace), type.Name))
        ]);
        _responseTypes = new(ResolveResponseTypes);
        _generatedTypes = new(() =>
        [
            .. _generatedTypeSeed.Value,
            .. SemanticCratisAdmission.GeneratedResponseDeclarations(this, [.. _slices.Values]).Select(type =>
                new GeneratedTypeName(CatalogNames.Qualify(RootNamespace, type.Namespace), type.Name))
        ]);
    }

    /// <summary>
    /// Gets the request being rendered.
    /// </summary>
    public ArtifactRenderRequest Request { get; }

    /// <summary>
    /// Gets the semantic application.
    /// </summary>
    public SemanticApplication Application { get; }

    /// <summary>
    /// Gets the destination-independent application namespace.
    /// </summary>
    public string RootNamespace { get; }

    /// <summary>
    /// Gets the optional validated locale catalog.
    /// </summary>
    public StringsCatalogInput.Catalog? Strings { get; }

    /// <summary>
    /// Gets concepts by semantic identity.
    /// </summary>
    public IReadOnlyDictionary<SemanticId, SemanticConcept> Concepts { get; }

    /// <summary>
    /// Gets composite types by semantic identity.
    /// </summary>
    public IReadOnlyDictionary<SemanticId, SemanticCompositeType> Types { get; }

    /// <summary>
    /// Gets event contracts by semantic identity.
    /// </summary>
    public IReadOnlyDictionary<SemanticId, SemanticEventContract> Events { get; }

    /// <summary>
    /// Gets commands by semantic identity.
    /// </summary>
    public IReadOnlyDictionary<SemanticId, SemanticCommand> Commands { get; }

    /// <summary>
    /// Gets read models by semantic identity.
    /// </summary>
    public IReadOnlyDictionary<SemanticId, SemanticReadModel> ReadModels { get; }

    /// <summary>
    /// Gets projections by semantic identity.
    /// </summary>
    public IReadOnlyDictionary<SemanticId, SemanticProjection> Projections { get; private set; }

    /// <summary>Gets reducer definitions across all slices.</summary>
    public IReadOnlyList<SemanticReducer> Reducers { get; }

    /// <summary>
    /// Gets queries by semantic identity.
    /// </summary>
    public IReadOnlyDictionary<SemanticId, SemanticKeyedQuery> Queries { get; }

    /// <summary>
    /// Gets specifications by semantic identity.
    /// </summary>
    public IReadOnlyDictionary<SemanticId, SemanticSpecification> Specifications { get; }

    /// <summary>
    /// Gets specification identities explicitly omitted from this render.
    /// </summary>
    public ImmutableHashSet<SemanticId> RefusedSpecifications { get; set; } = [];

    /// <summary>
    /// Gets concept identities used as modeled runtime identifiers.
    /// </summary>
    public IReadOnlySet<SemanticId> IdentifierConcepts { get; }

    /// <summary>
    /// Gets every append-time constraint in the application with the slice declaring it.
    /// </summary>
    public IReadOnlyList<(SemanticSlice Slice, SemanticConstraint Constraint)> Constraints { get; }

    /// <summary>
    /// Gets the identities of the queries a performer answers, which the application lists beside the declared ones.
    /// </summary>
    internal IReadOnlySet<SemanticId> PerformedQueries { get; }

    /// <summary>
    /// Gets why a performed query cannot render, by the identity of the read model it returns.
    /// </summary>
    internal IReadOnlyDictionary<SemanticId, string> PerformedQueryRejections { get; }

    internal GeneratedMemberNames MemberNames { get; }

    internal List<ArtifactRenderDiagnostic> InlineDiagnostics { get; } = [];

    internal IReadOnlyList<GeneratedTypeName> GeneratedTypes => _generatedTypes.Value;

    internal IReadOnlyList<LocatedSemanticSlice> AllSlices => [.. _slices.Values];

    internal ReducerConvention ReducerConvention { get; }

    internal IReadOnlyList<string> Domain { get; }
    internal string CommonFolder => string.Join('/', Domain.Append("Common"));
    internal string CommonNamespace => string.Join('.', new[] { RootNamespace }.Concat(Domain).Append("Common"));
    internal string PoliciesFolder => string.Join('/', Domain.Append("GeneratedPolicies"));
    internal string PoliciesNamespace => string.Join('.', new[] { RootNamespace }.Concat(Domain).Append("GeneratedPolicies"));
    internal string TypedContextsFolder => string.Join('/', Domain.Append("TypedContexts"));
    internal string TypedContextsNamespace => string.Join('.', new[] { RootNamespace }.Concat(Domain).Append("TypedContexts"));

    internal AuthoringMetadataInput.Catalog? Documentation { get; }

    internal Dictionary<string, IReadOnlySet<string>> ReducerContextReads { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets the admitted opaque policy bodies' context reads, by requirement and use site.</summary>
    internal Dictionary<(string RequirementId, SemanticId Operation), IReadOnlySet<string>> PolicyContextReads { get; } = [];

    /// <summary>Gets distinct generated namespace prefixes, including slices outside the selected scope.</summary>
    internal IEnumerable<string> NamespacePaths => _slices.Values.SelectMany(located =>
    {
        var segments = RootNamespace.Split('.').Concat(located.Path.Select(Identifiers.ToPascalCase)).ToArray();
        return Enumerable.Range(1, segments.Length).Select(length => string.Join('.', segments.Take(length)));
    }).Distinct(StringComparer.Ordinal);

    /// <summary>
    /// Gets the slices selected by the request scope in deterministic model order.
    /// </summary>
    /// <returns>The selected slices.</returns>
    public IReadOnlyList<LocatedSemanticSlice> SelectedSlices() =>
        [.. _slices.Values.Where(slice => new[] { Request.Scope }.Concat(Request.AdditionalScopes).Any(scope => scope.Kind switch
        {
            ArtifactRenderScopeKind.Application => true,
            ArtifactRenderScopeKind.Module => slice.Module.Id == scope.Artifact,
            ArtifactRenderScopeKind.Feature => slice.FeaturePath.Any(feature => feature.Id == scope.Artifact),
            ArtifactRenderScopeKind.Slice => slice.Slice.Id == scope.Artifact,
            _ => false
        }))];

    /// <summary>
    /// Gets the location of a semantic slice.
    /// </summary>
    /// <param name="slice">The slice identity.</param>
    /// <returns>The located slice.</returns>
    public LocatedSemanticSlice Slice(SemanticId slice) => _slices[slice];

    /// <summary>
    /// Gets the location of the slice declaring an artifact.
    /// </summary>
    /// <param name="artifact">The artifact identity.</param>
    /// <returns>The located slice.</returns>
    public LocatedSemanticSlice DeclaringSlice(SemanticId artifact) =>
        _slices.Values.Single(_ => _.Declares(artifact));

    /// <summary>
    /// Creates a builder for a generated application file, which spells the qualified types it is given by their short
    /// names wherever that provably binds the same and decides the file's using directives.
    /// </summary>
    /// <returns>The builder.</returns>
    internal CSharpCodeBuilder CodeBuilder() => new(GeneratedProjects.Application, GeneratedTypes, Request.Scope.Kind == ArtifactRenderScopeKind.Application);

    /// <summary>
    /// Creates a builder for a generated specification file.
    /// </summary>
    /// <returns>The builder.</returns>
    internal CSharpCodeBuilder SpecificationBuilder() => new(GeneratedProjects.Specifications, GeneratedTypes, Request.Scope.Kind == ArtifactRenderScopeKind.Application);

    internal SemanticCaptureMemberPlan CaptureMembers(SemanticCapture capture)
    {
        if (!_captureMembers.TryGetValue(capture.Id, out var plan))
        {
            plan = SemanticCaptureMemberPlan.Create(capture, this);
            _captureMembers.Add(capture.Id, plan);
        }
        return plan;
    }

    internal AuthoringMetadataInput.Metadata Docs(SemanticId id) =>
        Documentation?.Entries.GetValueOrDefault(id.ToString()) ?? new(null, null);

    internal SemanticReducerBindingEnvironment ReducerBindings(LocatedSemanticSlice located, SemanticReducer? reducer = null)
    {
        var classic = reducer is null || SelfReduces(reducer.ReadModel) ? null : reducer;
        var key = (located.Slice, classic?.ReadModel);
        if (!_reducerBindings.TryGetValue(key, out var environment))
        {
            environment = SemanticReducerBindingEnvironment.Create(located, this, classic);
            _reducerBindings.Add(key, environment);
        }

        return environment;
    }

    /// <summary>
    /// Gets the generated record name of a command's named response, escaped when it collides with a sibling
    /// declaration, a generated namespace or one of its own fields. The decision spans every slice, so it does not
    /// depend on the selected render scope.
    /// </summary>
    /// <param name="command">The command returning a named response.</param>
    /// <returns>The unqualified C# type name.</returns>
    internal string ResponseType(SemanticCommand command) => _responseTypes.Value[command.Id];

    internal bool SelfReduces(SemanticId readModel) => ReducerConvention.SelfReduces(readModel);

    internal string? PinnedObserverId(SemanticId readModel) => ReducerConvention.PinnedObserverId(readModel);

    internal IEnumerable<SemanticReducer> SelfReducers(LocatedSemanticSlice located) =>
        located.Slice.Reducers.Where(reducer => SelfReduces(reducer.ReadModel));

    // A flat projection the flat rendering cannot take is expressed as the scope that says the same, so one admission and one renderer decide.
    internal void ScopeFlatProjections()
    {
        foreach (var (id, located) in _slices.ToArray())
        {
            if (located.Slice.Projections.IsDefaultOrEmpty || !located.Slice.Projections.Any(NeedsScope))
            {
                continue;
            }

            _slices[id] = located with
            {
                Slice = located.Slice with
                {
                    Projections = [.. located.Slice.Projections.Select(projection => NeedsScope(projection) && RenderableScope(projection) is { } scope
                        ? projection with { Transitions = [], Scope = scope }
                        : projection)]
                }
            };
        }

        Projections = IndexIdentities(_slices.Values.SelectMany(_ => _.Slice.Projections), _ => _.Id, "projection");

        // Only a projection whose scope the scoped admission takes is converted: any other keeps its flat refusal.
        SemanticProjectionScope? RenderableScope(SemanticProjection projection) =>
            SemanticFlatProjectionScopes.ToScope(projection) is { } scope &&
            SemanticScopedProjectionSupport.Rejection(scope, this, ReadModels[projection.ReadModel].Properties) is null ? scope : null;

        bool NeedsScope(SemanticProjection projection) =>
            projection.Scope is null && !projection.Transitions.IsEmpty && projection.Target == SemanticProjectionTargetKind.ReadModel &&
            ReadModels.TryGetValue(projection.ReadModel, out var readModel) && readModel.Properties.Count(property => property.IsIdentifier) == 1 &&
            !SemanticCratisAdmission.FlatProjectionRenders(this, projection, readModel);
    }

    static Dictionary<SemanticId, T> IndexIdentities<T>(IEnumerable<T> declarations, Func<T, SemanticId> identity, string kind)
    {
        var index = new Dictionary<SemanticId, T>();
        foreach (var declaration in declarations)
        {
            var id = identity(declaration);
            if (!index.TryAdd(id, declaration))
            {
                throw UnsupportedSemanticRendering.For($"duplicate {kind} identity", id);
            }
        }
        return index;
    }

    static IReadOnlyList<SemanticFeature> FindFeaturePath(SemanticFeature feature, SemanticId slice)
    {
        if (feature.Slices.Any(_ => _.Id == slice))
        {
            return [feature];
        }

        foreach (var nested in feature.Features)
        {
            var path = FindFeaturePath(nested, slice);
            if (path.Count > 0)
            {
                return [feature, .. path];
            }
        }

        return [];
    }

    void IndexFeature(SemanticModule module, SemanticFeature feature, IReadOnlyList<string> parentPath)
    {
        var featurePath = parentPath.Append(feature.Name).ToArray();

        foreach (var slice in feature.Slices)
        {
            var parents = FindFeaturePath(module, slice.Id);
            _slices.Add(slice.Id, new(module, parents, slice, [.. featurePath, slice.Name]));
        }

        foreach (var nested in feature.Features)
        {
            IndexFeature(module, nested, featurePath);
        }
    }

    IReadOnlyList<SemanticFeature> FindFeaturePath(SemanticModule module, SemanticId slice) =>
        module.Features.Select(feature => FindFeaturePath(feature, slice)).First(_ => _.Count > 0);

    HashSet<SemanticId> FindIdentifierConcepts() =>
        [.. _slices.Values
            .SelectMany(_ => _.Slice.Commands.SelectMany(command => command.Properties)
                .Concat(_.Slice.ReadModels.SelectMany(readModel => readModel.Properties)))
            .Where(_ => _.IsIdentifier && _.Type.Kind == SemanticTypeReferenceKind.Concept)
            .Select(_ => _.Type.Target)
            .Concat(Request.Model.SemanticVersion.Major == SemanticVersion.V32.Major
                ? _slices.Values.Where(located => !located.Slice.Captures.IsDefaultOrEmpty)
                    .SelectMany(located => located.Slice.Captures).SelectMany(capture => capture.Appends)
                    .Where(append => append.EventSourceType.Kind == SemanticTypeReferenceKind.Concept)
                    .Select(append => append.EventSourceType.Target)
                : [])];

    Dictionary<SemanticId, string> ResolveResponseTypes()
    {
        var declared = _generatedTypeSeed.Value.Select(type => $"{type.Namespace}.{type.Name}").ToHashSet(StringComparer.Ordinal);
        var namespaces = NamespacePaths.ToHashSet(StringComparer.Ordinal);
        var names = new Dictionary<SemanticId, string>();
        foreach (var located in _slices.Values)
        {
            var ns = SliceNaming.Namespace(RootNamespace, located.Path);
            foreach (var command in located.Slice.Commands)
            {
                if (command.Response is not SemanticRecordCommandResponse response) continue;
                names[command.Id] = GeneratedPascalCase.ResponseType(command.Name, response.Fields.Select(field => field.Name), name =>
                    declared.Contains($"{ns}.{name}") || namespaces.Contains($"{ns}.{name}"));
            }
        }

        return names;
    }
}
