// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Api;

/// <summary>
/// An <see cref="IQueryPerformer"/> for a modeled read model query.
/// </summary>
/// <remarks>
/// Stage does not yet receive an executable query authorization contract from Screenplay. Queries therefore deny
/// anonymous access and authorization by default, so Arc short-circuits before <see cref="Perform"/> and exposes no
/// data. Full query authorization and execution depend on the Screenplay-owned executable semantic/query model;
/// this performer does not invent an interim query DTO contract.
/// </remarks>
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
    public bool AllowsAnonymousAccess => false;

    /// <inheritdoc/>
    public bool SupportsPaging => false;

    /// <inheritdoc/>
    public bool IsAuthorized(QueryContext context) => false;

    /// <inheritdoc/>
    public ValueTask<object?> Perform(QueryContext context)
    {
        object? data = _byId ? null : Array.Empty<DynamicReadModel>();

        return ValueTask.FromResult(data);
    }
}
