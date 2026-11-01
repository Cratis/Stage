// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Cryptography;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Execution.for_BodyUnitCompiler;

public class when_rejecting_cached_reducer_mismatches : Specification
{
    string _directory = null!;

    void Establish() => _directory = Directory.CreateTempSubdirectory("stage-reducer-admission-spec-").FullName;
    void Destroy() => Directory.Delete(_directory, true);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    async Task should_run_final_admission_before_reading_an_exact_populated_cache(bool mismatch)
    {
        var loaded = await when_rendering_a_pure_reducer.Load(when_rendering_a_pure_reducer.WithIdentifierConcept(when_rendering_a_pure_reducer.Source));
        var context = BodyUnitCompiler.CreateContext(loaded);
        var requirement = Assert.Single(loaded.ImplementationRequirements);
        var descriptor = Assert.Single(loaded.TypedContextDescriptors);
        var (_, body, refusal) = BodyUnitAdmission.AdmitReducer(context, requirement);
        Assert.Null(refusal);
        var unit = BodyUnitSources.RenderReducer(context, descriptor, body);
        var admitted = BodyUnitCompiler.Compile(loaded, _directory).Entries.Single();
        Assert.True(admitted.Admitted, admitted.DiagnosticMessage);
        var cached = BodyUnitCompiler.Compile(loaded, _directory, () => Assert.Fail("An admitted cache hit must not emit.")).Entries.Single();
        Assert.Equal(unit.EntryType, cached.EntryType);
        Assert.Equal(unit.EntryMethod, cached.EntryMethod);
        Assert.Equal(admitted.UnitHash, cached.UnitHash);
        Assert.Equal(admitted.PeSha256, cached.PeSha256);

        var sources = unit.Sources.ToArray();
        if (mismatch)
        {
            Assert.Contains(body, sources[0], StringComparison.Ordinal);
            sources[0] = sources[0].Replace(body, "return System.DateTime.UtcNow == default ? null! : null!;", StringComparison.Ordinal);
        }
        var invalid = mismatch ? unit with { Sources = sources } : unit with { Path = "worker/missing-helper.cs" };
        if (!mismatch) invalid = invalid with { Sources = [.. invalid.Sources, unit.Sources[0]] };
        MetadataReference[] references = [.. PureTransitionAdmission.AnalysisReferences(), SemanticGenerationBodyAdmission.FundamentalsReference, SemanticGenerationBodyAdmission.ChronicleReference];
        var hash = BodyUnitCompiler.ReducerHash(context, descriptor, invalid, references);
        Assert.NotEqual(admitted.UnitHash, hash);
        var bytes = await File.ReadAllBytesAsync(BodyUnitCache.PePath(_directory, admitted.UnitHash));
        var poisoned = admitted with { UnitHash = hash };
        BodyUnitCache.Write(_directory, poisoned, bytes);
        Assert.NotNull(BodyUnitCache.Read(_directory, hash));
        var manifest = Path.Combine(_directory, $"{hash}.json");
        var before = await File.ReadAllBytesAsync(manifest);
        var result = BodyUnitCompiler.CompileReducer(context, requirement, _directory, (() => Assert.Fail("A final mismatch must not emit."), (_, _, _) => invalid));
        Assert.False(result.Admitted);
        Assert.NotNull(result.DiagnosticCode);
        Assert.Equal(before, await File.ReadAllBytesAsync(manifest));
        Assert.Equal(Convert.ToHexString(SHA256.HashData(bytes)), poisoned.PeSha256);
        Assert.Equal("refused", BodyUnitInvoker.Evaluate(_directory, result, new(requirement.RequirementId, [])).Outcome);
    }

    [Fact]
    async Task should_run_original_admission_before_a_populated_cache_hit()
    {
        var loaded = await when_rendering_a_pure_reducer.Load(when_rendering_a_pure_reducer.WithIdentifierConcept(when_rendering_a_pure_reducer.Source));
        var entry = BodyUnitCompiler.Compile(loaded, _directory).Entries.Single();
        Assert.True(entry.Admitted, entry.DiagnosticMessage);
        var requirement = Assert.Single(loaded.ImplementationRequirements);
        var invalid = loaded with { ImplementationContents = loaded.ImplementationContents.SetItem(requirement.RequirementId, "return null!;") };
        var result = BodyUnitCompiler.CompileReducer(
            BodyUnitCompiler.CreateContext(invalid),
            requirement,
            _directory,
            (() => Assert.Fail("Original refusal must not emit."), (_, _, _) => throw new InvalidOperationException("Original refusal must precede rendering and cache lookup.")));
        Assert.False(result.Admitted);
        Assert.Equal("STAGE-ESM-056", result.DiagnosticCode);
        Assert.NotNull(BodyUnitCache.Read(_directory, entry.UnitHash));
    }
}
#endif
