// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Arc.Queries;
using Cratis.Stage.Contracts;

namespace Cratis.Stage.Api;

/// <summary>
/// Provides query performers to Arc by convention — for every read model in the event model it exposes a
/// <c>Get&lt;ReadModel&gt;ById</c> and an <c>All&lt;ReadModels&gt;</c> query.
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

            _performers.Add(new StageQueryPerformer(readModelType, $"Get{name}ById", located.Location, byId: true));
            _performers.Add(new StageQueryPerformer(readModelType, $"All{ModelNaming.Pluralize(name)}", located.Location, byId: false));
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
