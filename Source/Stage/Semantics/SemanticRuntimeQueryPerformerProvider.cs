// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Arc.Queries;
using Cratis.Stage.Api;
using Microsoft.AspNetCore.Http;

namespace Cratis.Stage.Semantics;

/// <summary>
/// Supplies compatibility and keyed query performers for a semantic runtime.
/// </summary>
public sealed class SemanticRuntimeQueryPerformerProvider : IQueryPerformerProvider
{
    readonly List<IQueryPerformer> _performers = [];

    /// <summary>
    /// Creates the performers when the opt-in runtime is present.
    /// </summary>
    /// <param name="runtimes">The optional semantic runtime.</param>
    /// <param name="factories">The optional type factory.</param>
    /// <param name="contexts">The optional HTTP accessor.</param>
    public SemanticRuntimeQueryPerformerProvider(
        IEnumerable<ISemanticRuntime> runtimes,
        IEnumerable<DynamicTypeFactory> factories,
        IEnumerable<IHttpContextAccessor> contexts)
    {
        var runtime = runtimes.FirstOrDefault();
        var factory = factories.FirstOrDefault();
        var context = contexts.FirstOrDefault();
        if (runtime is null || factory is null || context is null)
        {
            return;
        }

        foreach (var located in SemanticModelWalker.Slices(runtime.Plan.Model))
        {
            foreach (var readModel in located.Slice.ReadModels)
            {
                var name = ModelNaming.ToIdentifier(readModel.Name);
                var type = factory.CreateReadModelType(located.TypeNamespace, name);
                _performers.Add(new SemanticRuntimeQueryPerformer(type, $"Get{name}ById", located.CanonicalLocation, runtime, readModel, null, context, true));
                _performers.Add(new SemanticRuntimeQueryPerformer(type, $"All{ModelNaming.Pluralize(name)}", located.CanonicalLocation, runtime, readModel, null, context, false));
            }

            foreach (var query in located.Slice.Queries)
            {
                var model = runtime.Plan.ReadModels[query.ReadModel];
                var name = ModelNaming.ToIdentifier(model.Name);
                var type = factory.CreateReadModelType(located.TypeNamespace, name);
                _performers.Add(new SemanticRuntimeQueryPerformer(type, ModelNaming.ToIdentifier(query.Name), located.CanonicalLocation, runtime, model, query, context, true));
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
