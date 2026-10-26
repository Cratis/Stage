// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_RenderCompilation.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_RenderCompilation;

public class when_classifying_compiler_failures : Specification
{
    const string Diagnostic = "Program.cs(1,8): error CS1029: #error: 'STAGE824_CORRUPTION'";
    compilation_result _corruption = null!;
    compilation_result[] _notCorruption = [];

    void Because()
    {
        var execution = new generated_execution_result("dotnet build App.slnx", ["build", "App.slnx"], "build", 1, false, false, Diagnostic, string.Empty, TimeSpan.FromSeconds(1), "captured-build.log", null);
        _corruption = new(new("App.slnx", ["App.csproj"], ["App.csproj"], ["when_app.cs"]), true, execution);
        _notCorruption =
        [
            _corruption with { ExactMaterialization = false },
            _corruption with { Execution = execution with { Phase = "startup", ExitCode = null, Failure = new IOException("dotnet missing") } },
            _corruption with { Execution = execution with { TimedOut = true } },
            _corruption with { Execution = execution with { Truncated = true } },
            _corruption with { Execution = execution with { Phase = "integrity", Failure = new IOException("artifact changed") } },
            _corruption with { Execution = execution with { Phase = "execution", Failure = new IOException("output read failed") } },
            _corruption with { Execution = execution with { ExitCode = 0 } },
            _corruption with { Execution = execution with { ExitCode = null } },
            _corruption with { Execution = execution with { StandardOutput = "error MSB1009: Project file does not exist." } },
            _corruption with { Execution = execution with { StandardOutput = "Program.cs: error CS1029: #error: 'OTHER_MARKER'" } },
            _corruption with { Execution = execution with { StandardOutput = "Program.cs: error CS1001: STAGE824_CORRUPTION" } },
            _corruption with { Execution = execution with { StandardOutput = "Other.cs: error CS1029: STAGE824_CORRUPTION" } },
            _corruption with { Execution = execution with { StandardOutput = "Program.cs: error CS1029: OTHER_MARKER\nOther.cs: STAGE824_CORRUPTION" } }
        ];
    }

    [Fact] void should_require_the_specific_nonzero_compiler_diagnostic() => Assert.True(_corruption.HasCorruptionDiagnostic("Program.cs"));
    [Fact] void should_not_treat_infrastructure_failures_or_unrelated_errors_as_corruption_proof() => Assert.All(_notCorruption, result => Assert.False(result.HasCorruptionDiagnostic("Program.cs")));
    [Fact] void should_never_treat_a_failed_compilation_as_a_success() => Assert.All(
        _notCorruption.Append(_corruption).Where(result => result.Execution.ExitCode != 0), result => Assert.False(result.Success));

    [Fact]
    void should_require_a_zero_exit_and_intact_completed_build_for_success()
    {
        var completed = _corruption with { Execution = _corruption.Execution with { ExitCode = 0, StandardOutput = string.Empty } };
        Assert.True(completed.Success);
        Assert.False((completed with { ExactMaterialization = false }).Success);
        Assert.False((completed with { Execution = completed.Execution with { TimedOut = true } }).Success);
        Assert.False((completed with { Execution = completed.Execution with { Truncated = true } }).Success);
        Assert.False((completed with { Execution = completed.Execution with { Phase = "integrity", Failure = new IOException("artifact changed") } }).Success);
    }
}
#endif
