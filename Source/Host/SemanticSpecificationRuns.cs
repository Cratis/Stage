// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Host;

internal static class SemanticSpecificationRuns
{
    internal const string Route = "/stage/semantic/specifications/run";
    static readonly SemaphoreSlim _processGate = new(1, 1);

    internal static void Map(WebApplication app, SemanticExecutionPlan plan, string modelPath) =>
        app.MapPost(Route, (SemanticSpecificationRunRequest request, ISpecificationRunProcess process, SemanticSpecificationProcessOptions options, CancellationToken cancellationToken) =>
            Run(plan, modelPath, request, process, options, app.Logger, cancellationToken));

    static async Task<IResult> Run(SemanticExecutionPlan plan, string modelPath, SemanticSpecificationRunRequest request, ISpecificationRunProcess process, SemanticSpecificationProcessOptions options, ILogger logger, CancellationToken cancellationToken)
    {
        if (request.Scopes is { Length: > 1000 })
        {
            return Results.BadRequest(new { details = "A run accepts at most 1000 scopes." });
        }
        var scopes = new HashSet<SemanticId>();
        foreach (var scope in request.Scopes ?? [])
        {
            if (!SemanticId.TryParse(scope, out var id))
            {
                return Results.BadRequest(new { details = $"Invalid semantic identity '{scope}'." });
            }
            scopes.Add(id);
        }

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(options.Timeout);
        var directory = Path.Combine(Path.GetTempPath(), $"stage-specifications-{Guid.NewGuid():N}");
        var acquired = false;
        try
        {
            await _processGate.WaitAsync(deadline.Token);
            acquired = true;
            Directory.CreateDirectory(directory);
            var output = Path.Combine(directory, "results.json");
            var start = new ProcessStartInfo("dotnet")
            {
                UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true,
                WorkingDirectory = directory, CreateNoWindow = true
            };
            foreach (var argument in new[] { Path.GetFullPath(options.RunnerPath), "--engine", "semantic", "--model", Path.GetFullPath(modelPath), "--output", output })
            {
                start.ArgumentList.Add(argument);
            }
            foreach (var scope in scopes)
            {
                start.ArgumentList.Add("--scope");
                start.ArgumentList.Add(scope.ToString());
            }

            // No live world, services or Chronicle transport reach the runner. Its report must match the
            // admitted plan revision, so changing disk sources after startup cannot produce a false pass.
            var result = await process.Run(start, deadline.Token);
            logger.SpecificationProcessFinished(result.ExitCode, result.Output, result.Error);
            if (result.ExitCode != 0 || !File.Exists(output))
            {
                return Failure("The specification runner did not produce a completed report.");
            }
            if (new FileInfo(output).Length > 16 * 1024 * 1024)
            {
                return Failure("The specification report exceeds the size limit.");
            }
            var report = SemanticSpecificationRunReportFile.Read(await File.ReadAllTextAsync(output, deadline.Token));
            if (!Valid(report, plan, scopes))
            {
                return Failure("The specification runner produced an invalid or mismatched report.");
            }
            return Results.Json(report, SemanticSpecificationRunReportFile.SerializerOptions);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Failure("The specification runner timed out.", StatusCodes.Status504GatewayTimeout);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or Win32Exception)
        {
            logger.SpecificationProcessFailed(exception);
            return Failure("The specification runner could not complete the run.");
        }
        finally
        {
            try
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
            }
            finally
            {
                if (acquired) _processGate.Release();
            }
        }
    }

    static bool Valid(SemanticSpecificationRunReport? report, SemanticExecutionPlan plan, HashSet<SemanticId> scopes)
    {
        if (report is null || report.SchemaVersion != "stage-spec-run/1" || report.ApplicationId != plan.Model.Application.Id.ToString() ||
            report.SemanticRevision != plan.Revision.ToString() || report.Results is null)
        {
            return false;
        }
        var known = new HashSet<SemanticId> { plan.Model.Application.Id };
        var expected = new HashSet<string>();
        foreach (var module in plan.Model.Application.Modules)
        {
            known.Add(module.Id);
            Select(module.Features, scopes.Count == 0 || scopes.Contains(plan.Model.Application.Id) || scopes.Contains(module.Id));
        }
        expected.UnionWith(scopes.Where(scope => !known.Contains(scope)).Select(scope => scope.ToString()));
        return report.Results.All(record => record is not null) && report.Results.Count == expected.Count && expected.SetEquals(report.Results.Select(record => record.SpecificationId)) &&
            report.Results.All(record => Enum.IsDefined(record.Outcome) && record.Failures is not null &&
                (record.Outcome != SemanticSpecificationOutcome.Unsupported || record.Unsupported is { Details.Length: > 0 }));

        void Select(IEnumerable<SemanticFeature> features, bool inherited)
        {
            foreach (var feature in features)
            {
                known.Add(feature.Id);
                foreach (var slice in feature.Slices)
                {
                    known.Add(slice.Id);
                    foreach (var specification in slice.Specifications)
                    {
                        known.Add(specification.Id);
                        if (inherited || scopes.Contains(feature.Id) || scopes.Contains(slice.Id) || scopes.Contains(specification.Id)) expected.Add(specification.Id.ToString());
                    }
                }
                Select(feature.Features, inherited || scopes.Contains(feature.Id));
            }
        }
    }

    static IResult Failure(string details, int status = StatusCodes.Status502BadGateway) =>
        Results.Problem(detail: details, statusCode: status, title: "Specification execution failed", extensions: new Dictionary<string, object?> { ["capability"] = "Specification" });
}

internal sealed record SemanticSpecificationRunRequest(string[]? Scopes = null);
