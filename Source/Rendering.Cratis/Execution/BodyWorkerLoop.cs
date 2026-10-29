// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Execution;

/// <summary>The pipe-only worker role, shared by the executable and the CLI's hidden verb.</summary>
public static class BodyWorkerLoop
{
    /// <summary>Runs a compile or execute role without opening any sockets.</summary>
    /// <param name="args">The --role compile or --role execute launch arguments.</param>
    /// <param name="input">The supervisor's request stream.</param>
    /// <param name="output">The response stream, reserved exclusively for protocol messages.</param>
    /// <param name="error">The bounded diagnostic stream.</param>
    /// <returns>The process exit code.</returns>
    public static async Task<int> Run(string[] args, TextReader input, TextWriter output, TextWriter error)
    {
        if (!ValidArguments(args)) return 9;
        var cancellationToken = CancellationToken.None;
        var previous = CultureInfo.CurrentCulture;
        var previousUi = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        try
        {
            await WriteAsync(output, new BodyWorkerHello("hello", BodyWorkerMessages.ProtocolVersion, typeof(BodyWorkerLoop).Assembly.GetName().Version!.ToString()), cancellationToken).ConfigureAwait(false);
            return await LoopAsync(args[1], input, output, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is OutOfMemoryException or System.Reflection.TargetInvocationException { InnerException: OutOfMemoryException or InsufficientExecutionStackException })
        {
            await error.WriteLineAsync(BodyWorkerMessages.OutOfMemoryMarker).ConfigureAwait(false);
            await error.FlushAsync(cancellationToken).ConfigureAwait(false);
            return 8;
        }
        catch (Exception exception) when (exception is IOException or JsonException or OperationCanceledException)
        {
            return 9;
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }

    static bool ValidArguments(string[] args) =>
        args.Length == 2 && args[0] == "--role" && (args[1] == "compile" || args[1] == "execute");

    static async Task<int> LoopAsync(string role, TextReader input, TextWriter output, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var line = await BodyWorkerPipe.TryReadLineAsync(input, 1024 * 1024, token).ConfigureAwait(false);
            if (line is null) return 0;
            var request = JsonSerializer.Deserialize<BodyWorkerRequest>(line, BodyWorkerMessages.Json);
            if (request is null) return 9;
            var response = await ExecuteAsync(role, request).ConfigureAwait(false);
            await WriteAsync(output, response, token).ConfigureAwait(false);
            if (role == "compile" || request.Kind == "shutdown") return 0;
        }
        return 0;
    }

    static async Task<BodyWorkerResponse> ExecuteAsync(string role, BodyWorkerRequest request)
    {
        try
        {
            return request.Kind switch
            {
                "compile" when role == "compile" && request.ModelPath is not null && request.CacheDirectory is not null => new(request.Id) { Manifest = BodyUnitCompiler.Compile(await SemanticModelLoader.LoadFromPathAsync(request.ModelPath, request.CatalogPath, request.ApplicationName).ConfigureAwait(false), request.CacheDirectory) },
                "evaluate" or "validate" or "reduce" when role == "execute" => Invoke(request),
                "shutdown" => new(request.Id),
                _ => Failure(request.Id)
            };
        }
        catch (Exception exception) when (exception is IOException or ArgumentException or InvalidOperationException or UnauthorizedAccessException)
        {
            return Failure(request.Id);
        }
    }

    static BodyWorkerResponse Invoke(BodyWorkerRequest request)
    {
        if (!HasInvocation(request)) return Failure(request.Id);
        if (!MatchesRole(request.Kind, request.Unit!.Role)) return Failure(request.Id);
        return new(request.Id) { Evaluation = BodyUnitInvoker.Evaluate(request.CacheDirectory!, request.Unit!, request.Evaluation!) };
    }

    static bool HasInvocation(BodyWorkerRequest request) => request.Unit is not null && request.Evaluation is not null && request.CacheDirectory is not null;

    static bool MatchesRole(string kind, string bodyRole) => bodyRole switch
    {
        nameof(global::Cratis.Screenplay.Semantics.SemanticImplementationRole.RulePredicate) => kind == "evaluate",
        nameof(global::Cratis.Screenplay.Semantics.SemanticImplementationRole.CommandValidation) or nameof(global::Cratis.Screenplay.Semantics.SemanticImplementationRole.ConceptValidation) => kind == "validate",
        nameof(global::Cratis.Screenplay.Semantics.SemanticImplementationRole.ReducerTransition) => kind == "reduce",
        _ => false
    };

    static BodyWorkerResponse Failure(string id) => new(id) { Evaluation = new("faulted", Diagnostic: BodyDiagnostics.Format(BodyDiagnostics.WorkerFailed, "Body worker request failed or is unsupported.")) };

    static async Task WriteAsync<T>(TextWriter writer, T message, CancellationToken token)
    {
        await writer.WriteLineAsync(JsonSerializer.Serialize(message, BodyWorkerMessages.Json).AsMemory(), token).ConfigureAwait(false);
        await writer.FlushAsync(token).ConfigureAwait(false);
    }
}
