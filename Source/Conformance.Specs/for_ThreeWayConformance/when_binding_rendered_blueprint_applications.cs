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
/// Renders every live blueprint application twice, in the fully qualified spelling and with resolved imports, and
/// proves that every C# file of the application and its specifications binds every name to the same symbol.
/// </summary>
public class when_binding_rendered_blueprint_applications
{
    public static TheoryData<string> Blueprints() =>
        [.. Directory.GetDirectories(Path.Combine(Root(), "Verification", "live-blueprints", "models")).Select(Path.GetFileName).Order(StringComparer.Ordinal)!];

    [Theory]
    [MemberData(nameof(Blueprints))]
    public async Task should_bind_every_name_as_the_qualified_spelling(string blueprint)
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

        var proof = QualifiedOutputProof.VerifyRendered(Files(qualified), Files(rendered));

        Assert.True(proof.Failures.Count == 0, string.Join('\n', proof.Failures.Take(20)));
        var errors = QualifiedOutputProof.CompilationErrors(Files(rendered));
        Assert.True(errors.Count == 0, string.Join('\n', errors.Take(10)));
        Assert.True(proof.ShortSpellings > 0, "The rendered application spells no type by a short name.");
    }

    // The scaffold's policy registration declares the partial method generated policies implement.
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
