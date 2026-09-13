// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle;
using Cratis.Chronicle.Contracts;
using Cratis.Stage.Runtime;
using Microsoft.Extensions.DependencyInjection;
using ChronicleReadModels = Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Stage.Api;

/// <summary>
/// An <see cref="IQueryPerformer"/> for a modeled read model query, reading the documents the modeled projection
/// built in the session's own in-memory Chronicle.
/// </summary>
/// <remarks>
/// <para>
/// Screenplay still owns the executable query authorization contract, so a modeled <c language="csharp">authorize</c> on a query
/// is not evaluated here. What made that a reason to expose nothing at all was the risk of answering a query the
/// model meant to restrict - and that risk does not exist in this host: a play session is a disposable sandbox
/// with its own in-memory kernel, holding only the events that session itself appended, reached solely through
/// Studio's authenticated proxy. Answering nothing there protects nothing; it only leaves the person who pressed
/// play looking at an empty page.
/// </para>
/// <para>
/// A rendered application - the Cratis renderer's output, which is the path to production - gets Arc's real
/// authorization against the real model. Nothing here relaxes that.
/// </para>
/// </remarks>
public sealed class StageQueryPerformer : IQueryPerformer
{
    const int MaximumInstances = 500;
    readonly bool _byId;
    readonly string _readModelIdentifier;

    /// <summary>
    /// Initializes a new instance of the <see cref="StageQueryPerformer"/> class.
    /// </summary>
    /// <param name="readModelType">The emitted runtime read model type.</param>
    /// <param name="readModelIdentifier">The identifier the read model is registered with in Chronicle.</param>
    /// <param name="queryName">The conventional query name (for example <c language="csharp">GetUserById</c> or <c language="csharp">AllUsers</c>).</param>
    /// <param name="location">The route location segments for the query.</param>
    /// <param name="byId">Whether the query fetches a single instance by identifier.</param>
    public StageQueryPerformer(Type readModelType, string readModelIdentifier, string queryName, IReadOnlyList<string> location, bool byId)
    {
        ReadModelType = readModelType;
        Type = readModelType;
        Name = queryName;
        FullyQualifiedName = $"{readModelType.FullName}.{queryName}";
        Location = location;
        _byId = byId;
        _readModelIdentifier = readModelIdentifier;
        Parameters = byId ? new QueryParameters { { "id", typeof(string) } } : QueryParameters.Empty;
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
    public async ValueTask<object?> Perform(QueryContext context)
    {
        var instances = await Instances(context);

        if (!_byId)
        {
            return instances;
        }

        var arguments = context.Arguments;
        var id = arguments is not null && arguments.TryGetValue("id", out var value) ? value?.ToString() : null;

        return id is null
            ? null
            : instances.Find(instance => string.Equals((instance as DynamicReadModel)?.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    List<object> Parse(IEnumerable<string> instances)
    {
        var parsed = new List<object>();
        foreach (var instance in instances)
        {
            try
            {
                if (JsonNode.Parse(instance) is not JsonObject document)
                {
                    continue;
                }

                // The kernel keeps its own bookkeeping on the document. It says nothing about the model, so it has
                // no business being rendered as one of the read model's properties.
                foreach (var property in document.Select(property => property.Key).Where(key => key.StartsWith("__", StringComparison.Ordinal)).ToArray())
                {
                    document.Remove(property);
                }

                if (document.Deserialize(ReadModelType) is { } typed)
                {
                    parsed.Add(typed);
                }
            }
            catch (JsonException)
            {
                // A document the kernel cannot hand back as JSON says nothing about the rest of them; showing the
                // ones that did parse beats failing the whole query.
            }
        }

        return parsed;
    }

    async Task<List<object>> Instances(QueryContext context)
    {
        var serviceProvider = context.ServiceProvider
            ?? throw new InvalidOperationException("The query context carries no services to read the read model with.");
        var client = serviceProvider.GetRequiredService<IChronicleClient>();
        var eventStoreName = serviceProvider.GetRequiredService<StageEventStoreName>();
        var eventStore = await client.GetEventStore(eventStoreName.Value);
        var services = ((IChronicleServicesAccessor)eventStore.Connection).Services;

        var response = await services.ReadModels.GetInstances(new ChronicleReadModels.GetInstancesRequest
        {
            EventStore = eventStore.Name,
            Namespace = EventStoreNamespaceName.Default,
            ReadModel = _readModelIdentifier,
            Page = 0,
            PageSize = MaximumInstances
        });

        return Parse(response.Instances);
    }
}
