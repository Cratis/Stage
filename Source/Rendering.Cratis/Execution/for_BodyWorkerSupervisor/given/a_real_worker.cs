// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Execution.for_BodyWorkerSupervisor.given;

internal sealed class a_real_worker
{
    internal a_real_worker()
    {
        var root = Path.GetFullPath("../../../../..", AppContext.BaseDirectory);
        CacheDirectory = Path.Combine(root, ".ai-work", "891", "specs", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(CacheDirectory);
        WorkerPath = Path.Combine(root, "Source", "Host", "bin", "Debug", "net10.0", "bodyworker", "Cratis.Stage.BodyWorker.dll");
        Assert.True(File.Exists(WorkerPath), "Build Host in Debug before running the real-worker specifications.");
    }

    internal string CacheDirectory { get; }
    internal string WorkerPath { get; }

    internal BodyWorkerSupervisor Create(int timeout = 1000) => new(new(WorkerPath, CacheDirectory) { TimeoutMs = timeout, HeapLimitMb = 96 });

    // Deliberately bypass model purity ONLY in the stress fixture: production never compiles these bodies.
    internal BodyUnitEntry StressUnit(string body)
    {
        var source = $$"""
            namespace Fixtures;
            public record Context(decimal Value);
            public static class Bodies { public static bool Evaluate(Context context) { {{body}} } }
            """;
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
        var compilation = CSharpCompilation.Create($"Stress_{hash}", [CSharpSyntaxTree.ParseText(source)], PureTransitionAdmission.AnalysisReferences(), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join("; ", emit.Diagnostics));
        var bytes = stream.ToArray();
        var entry = new BodyUnitEntry("fixture", nameof(SemanticImplementationRole.RulePredicate), true, UnitHash: hash, PeSha256: Convert.ToHexString(SHA256.HashData(bytes)), EntryType: "Fixtures.Bodies", EntryMethod: "Evaluate", WrapperType: "Fixtures.Context");
        BodyUnitCache.Write(CacheDirectory, entry, bytes);
        return entry;
    }

    internal BodyWorkerRequest Request(BodyUnitEntry unit) => new(Guid.NewGuid().ToString("N"), "evaluate")
    {
        Unit = unit,
        CacheDirectory = CacheDirectory,
        Evaluation = new(unit.RequirementId, new Dictionary<string, System.Text.Json.JsonElement> { ["Value"] = SemanticValueWire.Write(SemanticValue.Number(1)) }.ToImmutableDictionary())
    };
}
