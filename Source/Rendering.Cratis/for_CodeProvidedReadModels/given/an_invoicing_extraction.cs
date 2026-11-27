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
    string? _fixture;

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

    string FreshFixture()
    {
        var destination = Path.Combine(Path.GetTempPath(), $"stage-invoicing-fixture-{Guid.NewGuid():N}");
        _fixture = destination;
        Directory.CreateDirectory(destination);
        var source = Path.Combine(AppContext.BaseDirectory, "for_CodeProvidedReadModels", "Fixtures", "Invoicing");
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }

        return destination;
    }

    void Destroy()
    {
        if (_fixture is null) return;
        if (!Directory.Exists(_fixture)) return;
        Directory.Delete(_fixture, recursive: true);
    }
}
#endif
