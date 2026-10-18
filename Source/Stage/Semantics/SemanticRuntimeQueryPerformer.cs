// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Claims;
using System.Text.Json;
using Cratis.Arc.Queries;
using Cratis.Arc.Validation;
using Cratis.DependencyInjection;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Api;
using Microsoft.AspNetCore.Http;

namespace Cratis.Stage.Semantics;

/// <summary>
/// The exception that is thrown when a semantic query is rejected.
/// </summary>
/// <param name="message">The rejection details.</param>
public sealed class SemanticQueryRejected(string message) : Exception(message), IValidationFailure
{
    /// <inheritdoc/>
    public ValidationResult ValidationResult => Cratis.Arc.Validation.ValidationResult.Error(Message);
}

/// <summary>
/// The exception that is thrown when a semantic query cannot run.
/// </summary>
/// <param name="message">The unsupported details.</param>
public sealed class SemanticQueryUnsupported(string message) : Exception(message);

/// <summary>
/// Serves a semantic snapshot query or a compatibility lookup from the in-process world.
/// </summary>
[IgnoreConvention]
internal sealed class SemanticRuntimeQueryPerformer : IQueryPerformer
{
    readonly ISemanticRuntime _runtime;
    readonly SemanticReadModel _model;
    readonly SemanticKeyedQuery? _query;
    readonly IHttpContextAccessor _context;
    readonly bool _byId;
    readonly IReadOnlyList<string> _filters;

    internal SemanticRuntimeQueryPerformer(
        Type type,
        string name,
        IEnumerable<string> location,
        ISemanticRuntime runtime,
        SemanticReadModel model,
        SemanticKeyedQuery? query,
        IHttpContextAccessor context,
        bool byId,
        IReadOnlyList<string>? filters = null)
    {
        ReadModelType = type;
        Type = type;
        Name = name;
        FullyQualifiedName = $"{type.FullName}.{name}";
        Location = location;
        _runtime = runtime;
        _model = model;
        _query = query;
        _context = context;
        _byId = byId;
        _filters = filters ?? [];
        Parameters = byId ? new QueryParameters { { query?.Argument?.Name ?? "id", typeof(string) } } : [];
        foreach (var part in query?.KeyParts ?? [])
        {
            Parameters.Add(part.Name, typeof(string));
        }

        foreach (var filter in query?.Filters ?? [])
        {
            Parameters.Add(filter.Name, typeof(string));
        }
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
    public bool IsAuthorized(QueryContext context)
    {
        if (_runtime is ISemanticRuntimeStatus { FaultReason: not null })
        {
            return true;
        }

        if (_query is null)
        {
            return true;
        }

        var result = new SemanticEvaluator().Execute(
            _runtime.Plan,
            SemanticWorld.Empty,
            SemanticExecutionRequest.ForQueries([new(_query.Id, Key(context))]) with
            {
                Caller = SemanticCallers.From(_context.HttpContext?.User ?? new())
            });
        return result is not SemanticRejected { Category: SemanticRejectionCategory.Unauthorized };
    }

    /// <inheritdoc/>
    public async ValueTask<object?> Perform(QueryContext context)
    {
        var http = _context.HttpContext;
        var principal = http?.User ?? new();
        var answer = await Evaluate(context, principal, http);
        if (_query?.Delivery != SemanticQueryDelivery.Live || _runtime is not ISemanticWorldChanges changes)
        {
            return answer;
        }

        // A later answer is produced long after this request, so it reads with the caller captured now and has no
        // response left to annotate.
        Func<Task<object?>> read = async () => await Evaluate(context, principal, null);
        var many = _query.Cardinality == SemanticQueryCardinality.Many;
        var answerType = many ? typeof(IEnumerable<>).MakeGenericType(ReadModelType) : ReadModelType;

        return typeof(SemanticRuntimeQueryPerformer)
            .GetMethod(nameof(Live), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(answerType)
            .Invoke(this, [changes, read, many ? ToTypedArray(answer) : answer, many]);
    }

    // Arc streams what an ISubject<T> emits and types it by T, so the subject is built over the read model type the
    // way a rendered C# query declares it rather than over object.
    SemanticLiveQuery<T> Live<T>(ISemanticWorldChanges changes, Func<Task<object?>> read, T initial, bool many) =>
        new(changes, async () => (T)(many ? ToTypedArray(await read()) : (await read())!), initial);

    Array ToTypedArray(object? answer)
    {
        var items = (object[])answer!;
        var typed = Array.CreateInstance(ReadModelType, items.Length);
        for (var index = 0; index < items.Length; index++)
        {
            typed.SetValue(items[index], index);
        }

        return typed;
    }

    async ValueTask<object?> Evaluate(QueryContext context, ClaimsPrincipal principal, HttpContext? http)
    {
        if (_runtime is ISemanticRuntimeStatus { FaultReason: { } reason })
        {
            throw UnsupportedWorld(reason, http);
        }

        var argumentName = _query?.Argument?.Name ?? "id";
        var key = context.Arguments?.TryGetValue(argumentName, out var value) == true ? value?.ToString() : null;
        if (_query is not null)
        {
            var result = await Query(context, _query, principal);
            return result switch
            {
                SemanticAccepted accepted when _query.Cardinality == SemanticQueryCardinality.Many => accepted.Queries.Single().Results.Select(Convert).ToArray(),
                SemanticAccepted accepted => accepted.Queries.Single().Results.FirstOrDefault() is { } instance ? Convert(instance) : null,
                SemanticRejected rejected => throw new SemanticQueryRejected(rejected.Details),
                SemanticUnsupported unsupported => throw Unsupported(unsupported, http),
                _ => throw new SemanticQueryRejected("Semantic query did not complete.")
            };
        }

        var snapshot = await _runtime.ReadModels(_model.Id);
        if (_runtime is ISemanticRuntimeStatus { FaultReason: { } fault })
        {
            throw UnsupportedWorld(fault, http);
        }

        IReadOnlyList<object> instances = [.. snapshot.Select(Convert)];
        if (_byId)
        {
            return instances.FirstOrDefault(instance => instance is DynamicReadModel model && string.Equals(model.Id, key, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var filter in _filters)
        {
            if (ReadModelArgumentFilter.TryGetArgument(context.Arguments, filter, out var argument))
            {
                instances = ReadModelArgumentFilter.Matching(instances, filter, argument);
            }
        }

        return instances;
    }

    SemanticValue Key(QueryContext context)
    {
        if (_query is { KeyParts.IsDefaultOrEmpty: false })
        {
            return SemanticQueryKeys.FromParts(
                _query,
                name => context.Arguments?.TryGetValue(name, out var part) == true ? part?.ToString() : null,
                _runtime.Plan.Model.Application);
        }

        if (_query?.Argument is not { } argument)
        {
            return SemanticValue.Null;
        }

        var text = context.Arguments?.TryGetValue(argument.Name, out var value) == true ? value?.ToString() : null;
        return SemanticQueryKeys.From(text, argument.Type, _runtime.Plan.Model.Application);
    }

    Task<SemanticExecutionResult> Query(QueryContext context, SemanticKeyedQuery query, ClaimsPrincipal principal) => query.Filters.IsDefaultOrEmpty
            ? _runtime.Query(query, Key(context), principal)
            : _runtime.Query(query, Key(context), principal, Filters(context));

    ImmutableArray<SemanticQueryFilterValue> Filters(QueryContext context) =>
        [.. _query!.Filters.Where(filter => context.Arguments?.ContainsKey(filter.Name) == true)
            .Select(filter => new SemanticQueryFilterValue(
                filter.Id,
                SemanticQueryKeys.From(context.Arguments![filter.Name]?.ToString(), filter.Type, _runtime.Plan.Model.Application)))];

    SemanticQueryUnsupported UnsupportedWorld(string reason, HttpContext? http) => UnsupportedResponse("World", reason, http);

    SemanticQueryUnsupported Unsupported(SemanticUnsupported unsupported, HttpContext? http) => UnsupportedResponse(
        unsupported.Capability == SemanticExecutionCapability.Unknown ? "World" : unsupported.Capability.ToString(), unsupported.Details, http);

    SemanticQueryUnsupported UnsupportedResponse(string capability, string details, HttpContext? http)
    {
        var artifact = _query?.Id.ToString() ?? _model.Id.ToString();
        if (http is { } context)
        {
            context.Items[SemanticRuntimeMarkers.Unsupported] = true;
            context.Items[SemanticRuntimeMarkers.UnsupportedMessage] = $"Unsupported({capability}) {artifact}: {details}";
            context.Response.Headers["Stage-Unsupported-Capability"] = capability;
            context.Response.Headers["Stage-Unsupported-Artifact"] = artifact;
        }

        return new SemanticQueryUnsupported(details);
    }

    object Convert(SemanticReadModelInstance instance)
    {
        // A composite key has no single value; its identity is the object of its named parts.
        var id = SemanticQueryKeys.IsComposite(_model)
            ? SemanticQueryKeys.ToText(instance.Key, _model, _runtime.Plan.Model.Application)
            : SemanticJsonValues.ToObject(instance.Key)?.ToString() ?? string.Empty;
        var result = (DynamicReadModel)JsonSerializer.Deserialize(JsonSerializer.Serialize(new { Id = id }), ReadModelType)!;
        foreach (var property in instance.Values)
        {
            var definition = _model.Properties.Single(candidate => candidate.Id == property.TargetProperty);
            result.Values[definition.Name] = JsonSerializer.SerializeToElement(SemanticJsonValues.ToObject(property.Value, definition.Type, _runtime.Plan.Model.Application));
        }

        return result;
    }
}
