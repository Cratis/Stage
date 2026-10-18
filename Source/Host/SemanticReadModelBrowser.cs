// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Host;

internal static class SemanticReadModelBrowser
{
    internal const string Route = "/stage/read-models";

    internal static void Map(WebApplication app, ISemanticRuntime runtime, Func<bool> ready, Func<HttpContext, IResult?>? refusal = null) =>
        Map(app, new BrowserContext(runtime, ready, refusal));

    internal static void Map(WebApplication app, BrowserContext browser)
    {
        app.MapGet(Route, (Func<HttpContext, Task<IResult>>)(context => List(context, browser)));
        app.MapGet($"{Route}/{{id}}", (HttpContext context, string id) => Detail(context, id, browser));
    }

    internal static void MapEventModel(WebApplication app)
    {
        app.MapGet(Route, EventModelResponse);
        app.MapGet($"{Route}/{{id}}", (string id) => EventModelResponse());
    }

    static IResult EventModelResponse() => Results.Json(
        new
        {
            engine = "eventmodel",
            reason = "Read models are shown in the Chronicle Workbench under the eventmodel engine."
        },
        StageJson.Options,
        statusCode: StatusCodes.Status404NotFound);

    static async Task<IResult> List(HttpContext context, BrowserContext browser)
    {
        var runtime = browser.Runtime;
        if (Unavailable(context, runtime, browser.Ready, browser.Refusal) is { } unavailable)
        {
            return unavailable;
        }

        var models = new List<object>();
        foreach (var module in runtime.Plan.Model.Application.Modules)
        {
            foreach (var located in Locate(module.Features))
            {
                foreach (var model in located.Slice.ReadModels)
                {
                    models.Add(await Summary(browser, module.Name, located, model));
                }
            }
        }

        // A command may fault while this request waits for the runtime gate. Never present its empty
        // fallback as an authoritative count or return rows collected before that fault.
        return Fault(runtime) ?? Results.Json(new { readModels = models }, StageJson.Options);
    }

    static async Task<object> Summary(BrowserContext browser, string module, (string Feature, SemanticSlice Slice) located, SemanticReadModel model)
    {
        var runtime = browser.Runtime;
        if (browser.RefusedModels.TryGetValue(model.Id.ToString(), out var refused))
        {
            return new
            {
                id = model.Id.ToString(), model.Name, module,
                feature = located.Feature, slice = located.Slice.Name,
                properties = Properties(model, runtime.Plan.Model.Application), instances = (int?)null,
                unsupported = refused
            };
        }

        var instances = await runtime.ReadModels(model.Id);

        return new
        {
            id = model.Id.ToString(), model.Name, module,
            feature = located.Feature, slice = located.Slice.Name,
            properties = Properties(model, runtime.Plan.Model.Application), instances = instances.Length
        };
    }

    static async Task<IResult> Detail(HttpContext context, string id, BrowserContext browser)
    {
        var runtime = browser.Runtime;
        if (Unavailable(context, runtime, browser.Ready, browser.Refusal) is { } unavailable)
        {
            return unavailable;
        }
        if (!SemanticId.TryParse(id, out var identity) || !runtime.Plan.ReadModels.TryGetValue(identity, out var model))
        {
            return Results.NotFound();
        }

        if (browser.RefusedModels.TryGetValue(identity.ToString(), out var refused))
        {
            return SemanticHost.RefusedResponse(context, [new(refused.Capability!, refused.Artifact, refused.Details!)]);
        }

        var instances = await runtime.ReadModels(identity);
        var application = runtime.Plan.Model.Application;

        return Fault(runtime) ?? Results.Json(
            new
            {
                id = model.Id.ToString(), model.Name, properties = Properties(model, application),
                instances = instances.Select(instance => new
                {
                    key = SemanticQueryKeys.ToObject(instance.Key, model, application),
                    values = instance.Values.ToDictionary(
                        value => model.Properties.Single(property => property.Id == value.TargetProperty).Name,
                        value => SemanticJsonValues.ToObject(value.Value, model.Properties.Single(property => property.Id == value.TargetProperty).Type, application))
                })
            },
            StageJson.Options);
    }

    static IResult? Unavailable(HttpContext context, ISemanticRuntime runtime, Func<bool> ready, Func<HttpContext, IResult?>? refusal)
    {
        if (refusal?.Invoke(context) is { } refused)
        {
            return refused;
        }
        if (Fault(runtime) is { } fault)
        {
            return fault;
        }
        if (ready())
        {
            return null;
        }

        context.Response.Headers.RetryAfter = "1";

        return Results.Json(new { state = "loading" }, StageJson.Options, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    static IResult? Fault(ISemanticRuntime runtime) => runtime is ISemanticRuntimeStatus { FaultReason: { } reason }
        ? Results.Json(new { state = "faulted", reason }, StageJson.Options, statusCode: StatusCodes.Status503ServiceUnavailable)
        : null;

    static IEnumerable<object> Properties(SemanticReadModel model, SemanticApplication application) =>
        model.Properties.Select(property => new { property.Name, type = TypeName(property.Type, application) });

    static string TypeName(SemanticTypeReference type, SemanticApplication application)
    {
        var name = type.Kind switch
        {
            SemanticTypeReferenceKind.Concept => application.Concepts.Single(concept => concept.Id == type.Target).Name,
            SemanticTypeReferenceKind.CompositeType => application.Types.Single(composite => composite.Id == type.Target).Name,
            _ => type.Primitive.ToString()
        };

        return name + (type.IsOptional ? "?" : string.Empty) + (type.IsCollection ? "[]" : string.Empty);
    }

    static IEnumerable<(string Feature, SemanticSlice Slice)> Locate(IEnumerable<SemanticFeature> features, string parent = "") =>
        features.SelectMany(feature =>
        {
            var path = parent.Length == 0 ? feature.Name : $"{parent}/{feature.Name}";

            return feature.Slices.Select(slice => (path, slice)).Concat(Locate(feature.Features, path));
        });

    internal sealed record BrowserContext(ISemanticRuntime Runtime, Func<bool> Ready, Func<HttpContext, IResult?>? Refusal = null, SemanticRuntimeAdmission? Admission = null)
    {
        internal IReadOnlyDictionary<string, SemanticAdmissionEntry> RefusedModels { get; } =
            (Admission ?? new SemanticRuntimeAdmission(Runtime.Plan)).Entries
                .Where(entry => entry.Kind == "readModel" && entry.Status == "unsupported").ToDictionary(entry => entry.Artifact, StringComparer.Ordinal);
    }
}
