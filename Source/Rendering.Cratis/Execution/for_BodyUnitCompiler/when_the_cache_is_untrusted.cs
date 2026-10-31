// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Execution.for_BodyUnitCompiler;

public class when_the_cache_is_untrusted : Specification
{
    string _directory = null!;

    void Establish() => _directory = Directory.CreateTempSubdirectory("stage-cache-poison-spec-").FullName;
    void Destroy() => Directory.Delete(_directory, true);

    [Fact]
    void should_not_reuse_or_execute_a_self_consistent_entry_in_a_shared_cache()
    {
        if (OperatingSystem.IsWindows()) return;
        var loaded = validation_body_model.Load(validation_body_model.Command("return context.Value > 0;"));
        var entry = BodyUnitCompiler.Compile(loaded, _directory).Entries.Single();
        Assert.True(entry.Admitted, entry.DiagnosticMessage);
        var replacement = validation_body_model.Load(validation_body_model.Command("return true;"));
        var attacker = BodyUnitCompiler.Compile(replacement, Path.Combine(_directory, "attacker")).Entries.Single();
        var poisoned = attacker with { UnitHash = entry.UnitHash };
        BodyUnitCache.Write(_directory, poisoned, File.ReadAllBytes(BodyUnitCache.PePath(Path.Combine(_directory, "attacker"), attacker.UnitHash)));
        BodyUnitCache.Verify(_directory, poisoned).ShouldBeTrue();
        File.SetUnixFileMode(_directory, File.GetUnixFileMode(_directory) | UnixFileMode.OtherWrite);
        var emissions = 0;
        Assert.Throws<UnauthorizedAccessException>(() => BodyUnitCompiler.Compile(loaded, _directory, () => emissions++));
        emissions.ShouldEqual(0);
        Assert.Throws<UnauthorizedAccessException>(() => BodyUnitInvoker.Evaluate(_directory, entry, new(entry.RequirementId, [])));
    }

    [Fact]
    async Task should_return_an_explicit_worker_failure_instead_of_launching_from_a_shared_cache()
    {
        if (OperatingSystem.IsWindows()) return;
        File.SetUnixFileMode(_directory, File.GetUnixFileMode(_directory) | UnixFileMode.GroupWrite);
        await using var supervisor = new BodyWorkerSupervisor(new("unused-worker.dll", _directory));
        var manifest = await supervisor.CompileAsync("Model.play");
        var entry = Assert.Single(manifest.Entries);
        entry.Admitted.ShouldBeFalse();
        Assert.StartsWith(BodyDiagnostics.WorkerFailed, entry.DiagnosticMessage, StringComparison.Ordinal);
        supervisor.ProcessId.ShouldBeNull();
    }
}
