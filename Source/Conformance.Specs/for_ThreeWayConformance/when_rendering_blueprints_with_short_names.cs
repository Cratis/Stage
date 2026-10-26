// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Runtime.CompilerServices;
using System.Text;
using Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Renders every live blueprint application and measures how short its C# is spelled: a file with a known scope keeps
/// <c language="csharp">global::</c> only where two types share a name, and has no unused using directive. A file with
/// an unknown scope keeps every name qualified by design and is exempt.
/// </summary>
public class when_rendering_blueprints_with_short_names
{
    public static TheoryData<string> Blueprints() =>
        [.. Directory.GetDirectories(Path.Combine(Root(), "Verification", "live-blueprints", "models")).Select(Path.GetFileName).Order(StringComparer.Ordinal)!];

    [Theory]
    [MemberData(nameof(Blueprints))]
    public async Task should_spell_types_short_and_import_only_what_is_used(string blueprint)
    {
        var loaded = await SemanticModelLoader.LoadFromPathAsync(Path.Combine(Root(), "Verification", "live-blueprints", "models", blueprint), null, "Blueprint");
        var selection = new PlanSelection([.. loaded.Model.Application.Modules.Select(module => PlanSelectionEntry.Module(module.Name))]);
        var options = new CratisPlanOptions("Blueprint", "Blueprint", "Blueprint");
        var rendered = CratisRendering.PlanFrom(loaded, selection, options);
        Assert.True(rendered.Success, string.Join('\n', rendered.Diagnostics));

        CratisPlanResult qualified;
        using (QualifiedSpelling.Begin())
        {
            qualified = CratisRendering.PlanFrom(loaded, selection, options);
        }

        var measurement = QualifiedOutputProof.Measure(Files(rendered), Files(qualified));

        // The policy registration stub exists only so the generated policies compile; it is not rendered output.
        const string Stub = "GeneratedPolicies/Registration.cs";

        Assert.True(measurement.CompilationErrors.Count == 0, string.Join('\n', measurement.CompilationErrors.Take(10)));
        var unexplained = measurement.Residuals
            .Where(residual => residual.Path != Stub && !known_residual_qualifications.IsExplained(residual, measurement.UnknownScope.GetValueOrDefault(residual.Path)))
            .GroupBy(residual => $"{residual.Path} :: {residual.Namespace}.{residual.Name}")
            .Select(group => $"{group.Key} x{group.Count()}")
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.True(unexplained.Length == 0, $"{unexplained.Length} unexplained qualifications:\n{string.Join('\n', unexplained.Take(60))}");

        var unused = measurement.UnusedUsings.Where(entry => !entry.StartsWith(Stub, StringComparison.Ordinal)).Where(entry => measurement.UnknownScope.GetValueOrDefault(entry[..entry.IndexOf(':', StringComparison.Ordinal)]) == UnknownScopeReason.None).ToArray();
        Assert.True(unused.Length == 0, $"{unused.Length} unused usings in known-scope files:\n{string.Join('\n', unused.Take(40))}");
    }

    // Guards the measurement itself: a file that keeps a type qualified must be found, or the corpus theory passes vacuously.
    [Fact]
    public void should_find_a_qualification_a_known_scope_file_keeps()
    {
        RenderedFile[] files = [new("Shop/Placing.cs", "namespace Blueprint.Shop;\n\npublic class Placing\n{\n    public global::System.Guid Id { get; init; }\n    public global::System.Text.StringBuilder Text { get; init; } = new();\n    readonly string StringBuilder = string.Empty;\n}\n")];

        var measurement = QualifiedOutputProof.Measure(files, files);

        Assert.Equal(UnknownScopeReason.None, measurement.UnknownScope["Shop/Placing.cs"]);
        Assert.Equal(["Guid:False", "StringBuilder:True"], measurement.Residuals.Select(residual => $"{residual.Name}:{residual.Collides}").Order(StringComparer.Ordinal));
    }

    static RenderedFile[] Files(CratisPlanResult plan) => [.. plan.Artifacts
        .Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal))
        .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan()))),
        new("GeneratedPolicies/Registration.cs", """
            using Microsoft.Extensions.DependencyInjection;

            namespace Blueprint.GeneratedPolicies;

            public static partial class Registration
            {
                public static void Register(IServiceCollection services) => RegisterGenerated(services);

                static partial void RegisterGenerated(IServiceCollection services);
            }
            """)];

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Verification"))) directory = directory.Parent;
        return directory!.FullName;
    }
}
#endif
