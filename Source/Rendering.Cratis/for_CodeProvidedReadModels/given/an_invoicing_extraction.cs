// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CodeProvidedReadModels.given;

/// <summary>Renders the small file-backed extraction, retaining the loader's authenticated envelope.</summary>
public class an_invoicing_extraction : a_generated_application
{
    protected override ArtifactRenderPlan CreatePlan()
    {
        var root = FreshFixture();
        var compiled = SemanticModelLoader.LoadFromPathAsync(root, null, "Invoicing").GetAwaiter().GetResult();
        Assert.Empty(compiled.AttachmentDiagnostics);
        Assert.Contains(compiled.ImplementationContents.Values, body => body.Contains("return 50000;", StringComparison.Ordinal));
        Assert.Contains(compiled.ImplementationContents.Values, body => body.Contains("return 14;", StringComparison.Ordinal));
        var profile = CratisRendering.WithRealization(
            CratisRendering.CreateProfile("Invoicing", new("Invoicing", "Invoicing")), compiled.Realization);
        var request = new ArtifactRenderRequest(
            compiled.Model,
            compiled.Plan,
            profile,
            new(ArtifactRenderScopeKind.Application, compiled.Model.Application.Id))
        {
            ImplementationRequirements = compiled.ImplementationRequirements,
            ImplementationContents = compiled.ImplementationContents,
            TypedContextDescriptors = compiled.TypedContextDescriptors
        };
        return new CratisArtifactRenderPlanner().Plan(request);
    }

    static string FreshFixture()
    {
        DirectoryInfo? repository = new(AppContext.BaseDirectory);
        while (repository is not null)
        {
            if (IsRepository(repository)) break;
            repository = repository.Parent;
        }

        var root = repository?.FullName ?? throw new InvalidOperationException("Could not locate the fixture repository root.");
        var destination = Path.Combine(root, ".ai-work", "1146", "invoicing-fixture-" + Guid.NewGuid().ToString("N"));
        var source = Path.Combine(AppContext.BaseDirectory, "for_CodeProvidedReadModels", "Fixtures", "Invoicing");
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }

        return destination;
    }

    static bool IsRepository(DirectoryInfo directory)
    {
        var git = Path.Combine(directory.FullName, ".git");
        return File.Exists(git) || Directory.Exists(git);
    }
}
#endif
