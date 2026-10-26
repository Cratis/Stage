// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

namespace Cratis.Stage.Conformance.Specs.for_RenderCompilation.given;

public sealed record compilation_result(compilation_layout Layout, bool ExactMaterialization, generated_execution_result Execution)
{
    public bool Success => ExactMaterialization && Execution.Phase == "build" && Execution.Success;
    public string Summary => $"{Execution.Command}\nPhase: {Execution.Phase}; exit: {Execution.ExitCode?.ToString() ?? "not started"}; " +
        $"timeout: {Execution.TimedOut}; truncated: {Execution.Truncated}; elapsed: {Execution.Elapsed}; exact bytes: {ExactMaterialization}\n" +
        $"Solution: {Layout.Solution}; projects: {Layout.Projects.Count}; specification projects: {Layout.SpecificationProjects.Count}; specification sources: {Layout.SpecificationSources.Count}\n" +
        $"Projects: {string.Join(", ", Layout.Projects)}\nSpecification projects: {string.Join(", ", Layout.SpecificationProjects)}\n" +
        $"Log: {Execution.LogPath}\n{Execution.Failure}";
    public string Details => $"{Summary}\n{Execution.Output}";

    public bool HasCorruptionDiagnostic(string path) => ExactMaterialization && Execution.Phase == "build" &&
        Execution.ExitCode is not null and not 0 && Execution.Failure is null && !Execution.TimedOut && !Execution.Truncated &&
        Execution.Output.Split('\n').Any(line => line.Contains(Path.GetFileName(path), StringComparison.Ordinal) &&
            line.Contains("error CS1029", StringComparison.Ordinal) && line.Contains("STAGE824_CORRUPTION", StringComparison.Ordinal));
}
#endif
