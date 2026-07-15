// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Stage.Api;

/// <summary>
/// An <see cref="IQueryPerformer"/> for a modeled read model query. Foundation behavior returns no instance for
/// the by-id query and an empty set for the all query; reading projected documents from the read model store is a
/// follow-up.
/// </summary>
public sealed class StageQueryPerformer : IQueryPerformer
{
    readonly bool _byId;

    /// <summary>
    /// Initializes a new instance of the <see cref="StageQueryPerformer"/> class.
    /// </summary>
    /// <param name="readModelType">The emitted runtime read model type.</param>
    /// <param name="queryName">The conventional query name (for example <c>GetUserById</c> or <c>AllUsers</c>).</param>
    /// <param name="location">The route location segments for the query.</param>
    /// <param name="byId">Whether the query fetches a single instance by identifier.</param>
    public StageQueryPerformer(Type readModelType, string queryName, IReadOnlyList<string> location, bool byId)
    {
        ReadModelType = readModelType;
        Type = readModelType;
        Name = queryName;
        FullyQualifiedName = $"{readModelType.FullName}.{queryName}";
        Location = location;
        _byId = byId;
        Parameters = byId ? new QueryParameters { { "id", typeof(Guid) } } : QueryParameters.Empty;
    }

    /// <inheritdoc/>
    public QueryName Name { get; }

    /// <inheritdoc/>
    public FullyQualifiedQueryName FullyQualifiedName { get; }

    /// <inheritdoc/>
    public Type Type { get; }

    /// <inheritdoc/>
    public Type ReadModelType { get; }

    /// <inheritdoc/>
    public IEnumerable<string> Location { get; }

    /// <inheritdoc/>
    public string? CustomRoute => null;

    /// <inheritdoc/>
    public IEnumerable<Type> Dependencies => [];

    /// <inheritdoc/>
    public QueryParameters Parameters { get; }

    /// <inheritdoc/>
    public bool AllowsAnonymousAccess => true;

    /// <inheritdoc/>
    public bool SupportsPaging => false;

    /// <inheritdoc/>
    public bool IsAuthorized(QueryContext context) => true;

    /// <inheritdoc/>
    public ValueTask<object?> Perform(QueryContext context)
    {
        object? data = _byId ? null : Array.Empty<DynamicReadModel>();

        return ValueTask.FromResult(data);
    }
}
