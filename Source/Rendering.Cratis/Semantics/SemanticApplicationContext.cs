// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Indexes one executable semantic application by stable identity for direct Cratis rendering.
/// </summary>
internal sealed class SemanticApplicationContext
{
    readonly Dictionary<SemanticId, LocatedSemanticSlice> _slices = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticApplicationContext"/> class.
    /// </summary>
    /// <param name="request">The immutable artifact render request.</param>
    public SemanticApplicationContext(ArtifactRenderRequest request)
    {
        Request = request;
        Application = request.Model.Application;
        RootNamespace = Identifiers.ToPascalCase(Application.Name);
        Concepts = Application.Concepts.ToDictionary(_ => _.Id);
        Types = Application.Types.ToDictionary(_ => _.Id);

        foreach (var module in Application.Modules)
        {
            foreach (var feature in module.Features)
            {
                IndexFeature(module, feature, [module.Name]);
            }
        }

        Events = _slices.Values.SelectMany(_ => _.Slice.Events).ToDictionary(_ => _.Id);
        Commands = _slices.Values.SelectMany(_ => _.Slice.Commands).ToDictionary(_ => _.Id);
        ReadModels = _slices.Values.SelectMany(_ => _.Slice.ReadModels).ToDictionary(_ => _.Id);
        Projections = _slices.Values.SelectMany(_ => _.Slice.Projections).ToDictionary(_ => _.Id);
        Queries = _slices.Values.SelectMany(_ => _.Slice.Queries).ToDictionary(_ => _.Id);
        Specifications = _slices.Values.SelectMany(_ => _.Slice.Specifications).ToDictionary(_ => _.Id);
        IdentifierConcepts = FindIdentifierConcepts();
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
    public IReadOnlyDictionary<SemanticId, SemanticProjection> Projections { get; }

    /// <summary>
    /// Gets queries by semantic identity.
    /// </summary>
    public IReadOnlyDictionary<SemanticId, SemanticKeyedQuery> Queries { get; }

    /// <summary>
    /// Gets specifications by semantic identity.
    /// </summary>
    public IReadOnlyDictionary<SemanticId, SemanticSpecification> Specifications { get; }

    /// <summary>
    /// Gets concept identities used as modeled runtime identifiers.
    /// </summary>
    public IReadOnlySet<SemanticId> IdentifierConcepts { get; }

    /// <summary>
    /// Gets the slices selected by the request scope in deterministic model order.
    /// </summary>
    /// <returns>The selected slices.</returns>
    public IReadOnlyList<LocatedSemanticSlice> SelectedSlices() => Request.Scope.Kind switch
    {
        ArtifactRenderScopeKind.Application => [.. _slices.Values],
        ArtifactRenderScopeKind.Module => [.. _slices.Values.Where(_ => _.Module.Id == Request.Scope.Artifact)],
        ArtifactRenderScopeKind.Feature => [.. _slices.Values.Where(_ => _.FeaturePath.Any(feature => feature.Id == Request.Scope.Artifact))],
        ArtifactRenderScopeKind.Slice => [_slices[Request.Scope.Artifact]],
        _ => []
    };

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
            .Select(_ => _.Type.Target)];
}
