// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

    internal SemanticRuntimeQueryPerformer(
        Type type,
        string name,
        IEnumerable<string> location,
        ISemanticRuntime runtime,
        SemanticReadModel model,
        SemanticKeyedQuery? query,
        IHttpContextAccessor context,
        bool byId)
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
        Parameters = byId ? new QueryParameters { { query?.Argument.Name ?? "id", typeof(string) } } : QueryParameters.Empty;
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
        if (_runtime is ISemanticRuntimeStatus { FaultReason: { } reason })
        {
            throw UnsupportedWorld(reason);
        }

        var argumentName = _query?.Argument.Name ?? "id";
        var key = context.Arguments?.TryGetValue(argumentName, out var value) == true ? value?.ToString() : null;
        if (_query is not null)
        {
            var result = await _runtime.Query(_query, Key(context), _context.HttpContext?.User ?? new());
            return result switch
            {
                SemanticAccepted accepted => accepted.Queries.Single().Results.FirstOrDefault() is { } instance ? Convert(instance) : null,
                SemanticRejected rejected => throw new SemanticQueryRejected(rejected.Details),
                SemanticUnsupported unsupported => throw Unsupported(unsupported),
                _ => throw new SemanticQueryRejected("Semantic query did not complete.")
            };
        }

        var snapshot = await _runtime.ReadModels(_model.Id);
        if (_runtime is ISemanticRuntimeStatus { FaultReason: { } fault })
        {
            throw UnsupportedWorld(fault);
        }

        var instances = snapshot.Select(Convert).ToArray();
        return _byId
            ? instances.FirstOrDefault(instance => instance is DynamicReadModel model && string.Equals(model.Id, key, StringComparison.OrdinalIgnoreCase))
            : instances;
    }

    SemanticValue Key(QueryContext context)
    {
        var argument = _query!.Argument;
        var text = context.Arguments?.TryGetValue(argument.Name, out var value) == true ? value?.ToString() : null;
        return SemanticQueryKeys.From(text, argument.Type, _runtime.Plan.Model.Application);
    }

    SemanticQueryUnsupported UnsupportedWorld(string reason) => UnsupportedResponse("World", reason);

    SemanticQueryUnsupported Unsupported(SemanticUnsupported unsupported) => UnsupportedResponse(
        unsupported.Capability == SemanticExecutionCapability.Unknown ? "World" : unsupported.Capability.ToString(), unsupported.Details);

    SemanticQueryUnsupported UnsupportedResponse(string capability, string details)
    {
        var artifact = _query?.Id.ToString() ?? _model.Id.ToString();
        if (_context.HttpContext is { } context)
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
        var id = SemanticJsonValues.ToObject(instance.Key)?.ToString() ?? string.Empty;
        var result = (DynamicReadModel)JsonSerializer.Deserialize(JsonSerializer.Serialize(new { Id = id }), ReadModelType)!;
        foreach (var property in instance.Values)
        {
            var definition = _model.Properties.Single(candidate => candidate.Id == property.TargetProperty);
            result.Values[definition.Name] = JsonSerializer.SerializeToElement(SemanticJsonValues.ToObject(property.Value, definition.Type, _runtime.Plan.Model.Application));
        }

        return result;
    }
}
