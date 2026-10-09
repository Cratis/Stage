// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
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
    readonly string? _parameter;
    readonly bool _isCollection;
    readonly IReadOnlyList<string> _filters;

    /// <summary>
    /// Initializes a new instance of the <see cref="StageQueryPerformer"/> class.
    /// </summary>
    /// <param name="readModelType">The emitted runtime read model type.</param>
    /// <param name="readModelIdentifier">The identifier the read model is registered with in Chronicle.</param>
    /// <param name="queryName">The conventional query name (for example <c language="csharp">GetUserById</c> or <c language="csharp">AllUsers</c>).</param>
    /// <param name="location">The route location segments for the query.</param>
    /// <param name="byId">Whether the query fetches a single instance by identifier.</param>
    /// <param name="parameter">The read-model property a modeled query is narrowed by, or <see langword="null"/> for a conventional query.</param>
    /// <param name="isCollection">Whether a modeled query with a parameter returns every match rather than the first.</param>
    /// <param name="filters">The modeled <c language="csharp">by</c> parameters an unkeyed collection query honors when a caller supplies them.</param>
    public StageQueryPerformer(
        Type readModelType,
        string readModelIdentifier,
        string queryName,
        IReadOnlyList<string> location,
        bool byId,
        string? parameter = null,
        bool isCollection = true,
        IReadOnlyList<string>? filters = null)
    {
        ReadModelType = readModelType;
        Type = readModelType;
        Name = queryName;
        FullyQualifiedName = $"{readModelType.FullName}.{queryName}";
        Location = location;
        _byId = byId;
        _readModelIdentifier = readModelIdentifier;
        _parameter = parameter;
        _isCollection = isCollection;
        _filters = filters ?? [];
        Parameters = (byId, parameter) switch
        {
            (true, _) => new QueryParameters { { "id", typeof(string) } },
            (false, { } named) => new QueryParameters { { named, typeof(string) } },
            _ => QueryParameters.Empty
        };
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

        if (_parameter is not null)
        {
            ReadModelArgumentFilter.TryGetArgument(context.Arguments, _parameter, out var argument);
            var matching = ReadModelArgumentFilter.Matching(instances, _parameter, argument);

            if (_isCollection)
            {
                return matching;
            }

            return matching.Count > 0 ? matching[0] : null;
        }

        if (!_byId)
        {
            return Filtered(instances, context);
        }

        var arguments = context.Arguments;
        var id = arguments is not null && arguments.TryGetValue("id", out var value) ? value?.ToString() : null;

        return id is null
            ? null
            : instances.Find(instance => string.Equals((instance as DynamicReadModel)?.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    internal List<object> Parse(IEnumerable<string> instances)
    {
        var parsed = new List<object>();
        foreach (var instance in instances)
        {
            try
            {
                using var document = JsonDocument.Parse(instance);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidStageReadModelDocument(_readModelIdentifier);
                }

                var root = document.RootElement;
                var identityName = root.TryGetProperty("id", out var identity) ? "id" : "Id";
                if (identityName == "Id" && !root.TryGetProperty("Id", out identity))
                {
                    throw new InvalidStageReadModelDocument(_readModelIdentifier);
                }

                var id = Identity(identity);
                if (JsonSerializer.Deserialize(JsonSerializer.Serialize(new { Id = id }), ReadModelType) is not DynamicReadModel typed)
                {
                    throw new InvalidStageReadModelDocument(_readModelIdentifier);
                }

                foreach (var property in root.EnumerateObject().Where(property => property.Name != identityName && !property.Name.StartsWith("__", StringComparison.Ordinal)))
                {
                    typed.Values[property.Name] = property.Value.Clone();
                }

                parsed.Add(typed);
            }
            catch (JsonException exception)
            {
                throw new InvalidStageReadModelDocument(_readModelIdentifier, exception);
            }
        }

        return parsed;
    }

    IReadOnlyList<object> Filtered(IReadOnlyList<object> instances, QueryContext context)
    {
        // The conventional collection route is the fallback a screen reaches when it has no keyed route. A caller
        // naming a modeled by-parameter on it is asking for the narrowed set, so it is never handed the whole one.
        foreach (var filter in _filters)
        {
            if (ReadModelArgumentFilter.TryGetArgument(context.Arguments, filter, out var argument))
            {
                instances = ReadModelArgumentFilter.Matching(instances, filter, argument);
            }
        }

        return instances;
    }

    string Identity(JsonElement identity) => identity.ValueKind switch
    {
        JsonValueKind.String => identity.GetString()!,
        JsonValueKind.Number => Number(identity),
        JsonValueKind.True => bool.TrueString,
        JsonValueKind.False => bool.FalseString,
        JsonValueKind.Object => string.Join('_', identity.EnumerateObject().OrderBy(property => property.Name).Select(property => Identity(property.Value))),
        _ => throw new InvalidStageReadModelDocument(_readModelIdentifier)
    };

    string Number(JsonElement value)
    {
        var raw = value.GetRawText();
        if (decimal.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var decimalValue))
        {
            return decimalValue.ToString("G29", CultureInfo.InvariantCulture);
        }

        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue) && double.IsFinite(doubleValue))
        {
            return doubleValue.ToString("R", CultureInfo.InvariantCulture);
        }

        throw new InvalidStageReadModelDocument(_readModelIdentifier);
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
