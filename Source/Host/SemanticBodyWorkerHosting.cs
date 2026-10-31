// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Stage.Host;

internal static class SemanticBodyWorkerHosting
{
    internal static void AddRunnerArguments(ProcessStartInfo start, string[] arguments, SemanticBodyWorkerOptions? settings)
    {
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        AddBodyWorkerArguments(start, settings);
    }

    internal static void AddBodyWorkerArguments(ProcessStartInfo start, SemanticBodyWorkerOptions? settings)
    {
        if (settings is not { Enabled: true }) return;
        var worker = settings.Worker();
        foreach (var (name, value) in new[]
        {
            ("path", worker.WorkerPath), ("cache", worker.CacheDirectory), ("dotnet", worker.DotnetPath),
            ("timeout-ms", worker.TimeoutMs.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("compile-timeout-ms", worker.CompileTimeoutMs.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("heap-limit-mb", worker.HeapLimitMb.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("working-set-limit-mb", worker.WorkingSetLimitMb.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("max-response-bytes", worker.MaxResponseBytes.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("max-stderr-bytes", worker.MaxStderrBytes.ToString(System.Globalization.CultureInfo.InvariantCulture))
        })
        {
            start.ArgumentList.Add($"--body-worker-{name}");
            start.ArgumentList.Add(value);
        }
    }

    internal static async Task<SemanticBodyExecution?> Start(LoadedSemanticModel loaded, string modelPath, SemanticBodyWorkerOptions settings, List<SemanticModelLoadDiagnostic> notes)
    {
        if (!settings.Enabled || !loaded.ImplementationRequirements.Any(requirement => requirement.Role is SemanticImplementationRole.RulePredicate or SemanticImplementationRole.CommandValidation or SemanticImplementationRole.ConceptValidation or SemanticImplementationRole.ReducerTransition)) return null;
        var options = settings.Worker();
        if (!File.Exists(options.WorkerPath))
        {
            notes.Add(new(BodyDiagnostics.WorkerFailed, $"Body worker is unavailable at '{settings.WorkerPath}'. Rule bodies remain opaque."));
            return null;
        }
        await using var compiler = new BodyWorkerSupervisor(options);
        var manifest = await compiler.CompileAsync(modelPath, settings.CatalogPath, loaded.Model.Application.Name).ConfigureAwait(false);
        if (manifest.Revision != loaded.Plan.Revision.ToString())
        {
            notes.Add(new(BodyDiagnostics.WorkerFailed, manifest.Entries.FirstOrDefault()?.DiagnosticMessage ?? "Body worker returned a different model revision."));
            return null;
        }
        return SemanticBodyExecution.From(loaded, manifest, () => new BodyWorkerSupervisor(options, manifest));
    }
}
