// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;

public sealed record compile_error(string Code, string Path, int Line, string Message);

public static class rendered_compilation
{
    public static IReadOnlyList<compile_error> Errors(ArtifactRenderPlan plan) => Errors(plan.Artifacts);

    internal static IReadOnlyList<compile_error> Errors(IEnumerable<PlannedArtifact> artifacts)
    {
        var errors = Compilation(artifacts).GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Any(error => error.Location.SourceTree is null || error.Location.GetLineSpan().Path == "GlobalUsings.g.cs"))
        {
            throw new rendered_compilation_harness_error(string.Join(Environment.NewLine, errors.Select(error => error.ToString())));
        }

        return [.. errors.Select(diagnostic =>
            {
                var span = diagnostic.Location.GetLineSpan();
                return new compile_error(diagnostic.Id, span.Path, span.StartLinePosition.Line + 1, diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture));
            })
            .OrderBy(error => error.Path, StringComparer.Ordinal)
            .ThenBy(error => error.Line)
            .ThenBy(error => error.Code, StringComparer.Ordinal)
            .ThenBy(error => error.Message, StringComparer.Ordinal)];
    }

    internal static CSharpCompilation Compilation(IEnumerable<PlannedArtifact> artifacts)
    {
        var files = artifacts.ToArray();
        if (files.Count(file => file.RelativePath.EndsWith(".csproj", StringComparison.Ordinal)) != 1)
        {
            throw new rendered_compilation_needs_one_project();
        }

        var compilation = RenderedOutput.CreateCompilation(files
            .Where(file => file.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && !file.RelativePath.StartsWith("Frontend/", StringComparison.Ordinal))
            .Select(file => new RenderedFile(file.RelativePath, Encoding.UTF8.GetString(file.Bytes.AsSpan()))));

        // The generated app references the client SDK, not the kernel loaded by this test host.
        // The kernel duplicates client types such as IEventSequence; it is not a real app compile reference.
        return compilation
            .WithReferences(compilation.References.Where(reference => Path.GetFileName(reference.Display) != "Cratis.Chronicle.Core.dll"))
            .WithOptions(compilation.Options.WithOutputKind(OutputKind.ConsoleApplication));
    }

    sealed class rendered_compilation_harness_error(string details) : Exception(details);

    sealed class rendered_compilation_needs_one_project() : Exception("A published canonical render must contain exactly one C# project.");
}
#endif
