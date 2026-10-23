// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reactive.Subjects;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateViewArtifactRenderer;

/// <summary>
/// Test double for <see cref="IReadModels"/> that re-declares the default-implemented <c>Observe</c> as an overridable
/// member, so a substitute of it can be configured (NSubstitute cannot intercept default interface methods).
/// </summary>
public abstract class ObservableReadModels : IReadModels
{
    /// <inheritdoc/>
    public abstract IMaterializedReadModels Materialized { get; }

    /// <inheritdoc/>
    public abstract Task Register();

    /// <inheritdoc/>
    public abstract Task Register<TReadModel>();

    /// <inheritdoc/>
    public abstract Task<TReadModel> GetInstanceById<TReadModel>(ReadModelKey key, ReadModelSessionId? sessionId = null);

    /// <inheritdoc/>
    public abstract Task<object> GetInstanceById(Type readModelType, ReadModelKey key, ReadModelSessionId? sessionId = null);

    /// <inheritdoc/>
    public abstract Task<IEnumerable<TReadModel>> GetInstances<TReadModel>(EventCount? eventCount = null);

    /// <inheritdoc/>
    public abstract ISubject<IEnumerable<TReadModel>> Observe<TReadModel>(Expression<Func<TReadModel, bool>>? filter = null);

    /// <inheritdoc/>
    public abstract Task<IEnumerable<ReadModelSnapshot<TReadModel>>> GetSnapshotsById<TReadModel>(ReadModelKey readModelKey);

    /// <inheritdoc/>
    public abstract IObservable<ReadModelChangeset<TReadModel>> Watch<TReadModel>();

    /// <inheritdoc/>
    public abstract IReadModelWatcher<TReadModel> GetWatcherFor<TReadModel>();

    /// <inheritdoc/>
    public abstract Task DehydrateSession(ReadModelSessionId sessionId, Type readModelType, ReadModelKey readModelKey);

    /// <inheritdoc/>
    public abstract Task<TReadModel> Release<TReadModel>(TReadModel instance);

    /// <inheritdoc/>
    public abstract Task<IEnumerable<TReadModel>> Release<TReadModel>(IEnumerable<TReadModel> instances);
}
