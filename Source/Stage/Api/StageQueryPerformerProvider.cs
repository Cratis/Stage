// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Arc.Queries;
using Cratis.Stage.Contracts;

namespace Cratis.Stage.Api;

/// <summary>
/// Provides query performers to Arc by convention — for every read model in the event model it exposes a
/// <c language="csharp">Get&lt;ReadModel&gt;ById</c> and an <c language="csharp">All&lt;ReadModels&gt;</c> query, plus every
/// modeled query under its own name - narrowed by its <c language="csharp">by</c> parameter when it declares one.
/// </summary>
public sealed class StageQueryPerformerProvider : IQueryPerformerProvider
{
    readonly List<IQueryPerformer> _performers = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="StageQueryPerformerProvider"/> class. Arc discovers this
    /// provider in every host that references the Stage assembly, but only the Stage host registers an event model
    /// to run — so the dependencies are taken as optional collections and the provider exposes no performers when no
    /// event model is present.
    /// </summary>
    /// <param name="models">The event model the engine runs, when one is registered.</param>
    /// <param name="typeFactories">The factory used to emit a runtime type per read model, when one is registered.</param>
    public StageQueryPerformerProvider(IEnumerable<EventModel> models, IEnumerable<DynamicTypeFactory> typeFactories)
    {
        var model = models.FirstOrDefault();
        var typeFactory = typeFactories.FirstOrDefault();
        if (model is null || typeFactory is null)
        {
            return;
        }

        foreach (var located in StageModelWalker.Slices(model))
        {
            if (located.Slice.ReadModel is not { } readModel)
            {
                continue;
            }

            var name = ModelNaming.ToIdentifier(readModel.Name);
            var readModelType = typeFactory.CreateReadModelType(located.TypeNamespace, name);

            // The identifier the read model is registered in Chronicle with is what its documents are read back
            // by - see StageChronicleDefinitions, which registers exactly this identifier.
            var identifier = readModel.Id.ToString();

            var parameters = readModel.Queries
                .Select(query => query.Parameter)
                .OfType<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            // Every modeled query is served as itself, under its own name: an authored data binding names the
            // query it wants, and resolving it to another query over the same read model answers the wrong
            // question - a by-id lookup called with no argument shows an empty list. A modeled query that happens
            // to share a conventional name takes that name's place rather than registering it twice.
            var modeled = readModel.Queries
                .Select(query => (Name: ModelNaming.ToIdentifier(query.Name), Query: query))
                .DistinctBy(query => query.Name, StringComparer.Ordinal)
                .ToArray();
            var modeledNames = modeled.Select(query => query.Name).ToHashSet(StringComparer.Ordinal);
            var byIdName = $"Get{name}ById";
            var allName = $"All{ModelNaming.Pluralize(name)}";

            if (!modeledNames.Contains(byIdName))
            {
                _performers.Add(new StageQueryPerformer(readModelType, identifier, byIdName, located.CanonicalLocation, byId: true));
            }

            if (!modeledNames.Contains(allName))
            {
                _performers.Add(new StageQueryPerformer(readModelType, identifier, allName, located.CanonicalLocation, byId: false, filters: parameters));
            }

            foreach (var (queryName, query) in modeled)
            {
                _performers.Add(new StageQueryPerformer(
                    readModelType,
                    identifier,
                    queryName,
                    located.CanonicalLocation,
                    byId: false,
                    parameter: query.Parameter,
                    isCollection: query.IsCollection));
            }
        }
    }

    /// <inheritdoc/>
    public IEnumerable<IQueryPerformer> Performers => _performers;

    /// <inheritdoc/>
    public bool TryGetPerformerFor(FullyQualifiedQueryName query, [NotNullWhen(true)] out IQueryPerformer? performer)
    {
        performer = _performers.Find(candidate => candidate.FullyQualifiedName == query);

        return performer is not null;
    }
}
