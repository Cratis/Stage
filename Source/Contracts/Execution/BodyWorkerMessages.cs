// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Stage.Contracts.Execution;

/// <summary>The line protocol shared by the supervisor and target worker.</summary>
public static class BodyWorkerMessages
{
    /// <summary>The supported protocol version.</summary>
    public const int ProtocolVersion = 2;

    /// <summary>The bounded stderr marker used when the managed heap is exhausted.</summary>
    public const string OutOfMemoryMarker = "STAGE-BODY-WORKER-OOM";

    /// <summary>Gets the canonical wire serializer settings.</summary>
    public static JsonSerializerOptions Json { get; } = new(JsonSerializerDefaults.Web);
}

/// <summary>The worker's startup handshake.</summary>
/// <param name="Kind">The message kind.</param>
/// <param name="ProtocolVersion">The protocol version the worker speaks.</param>
/// <param name="WorkerVersion">The worker's own version.</param>
public sealed record BodyWorkerHello(string Kind, int ProtocolVersion, string WorkerVersion);

/// <summary>A correlated request, with exactly one operation on the pipe.</summary>
/// <param name="Id">The correlation identifier echoed by the response.</param>
/// <param name="Kind">The requested operation.</param>
public sealed record BodyWorkerRequest(string Id, string Kind)
{
    /// <summary>Gets the model path used only by the compile role.</summary>
    public string? ModelPath { get; init; }

    /// <summary>Gets the optional Screenplay catalog path used for this compilation.</summary>
    public string? CatalogPath { get; init; }

    /// <summary>Gets the application name used to preserve the loaded model's identity.</summary>
    public string? ApplicationName { get; init; }

    /// <summary>Gets the cache path holding compiled units.</summary>
    public string? CacheDirectory { get; init; }

    /// <summary>Gets the admitted unit identity for evaluation.</summary>
    public BodyUnitEntry? Unit { get; init; }

    /// <summary>Gets the invocation's tagged values.</summary>
    public BodyEvaluationRequest? Evaluation { get; init; }
}

/// <summary>A correlated compile or evaluation response.</summary>
/// <param name="Id">The correlation identifier of the request being answered.</param>
public sealed record BodyWorkerResponse(string Id)
{
    /// <summary>Gets the compilation result.</summary>
    public BodyUnitManifest? Manifest { get; init; }

    /// <summary>Gets the evaluation result.</summary>
    public BodyEvaluation? Evaluation { get; init; }
}
