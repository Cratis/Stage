// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Workspaces;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.Execution.for_BodyWorkerSupervisor.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Execution.for_ModelRootBodyExecution;

public class when_running_a_model_root : Specification
{
    [Fact]
    async Task should_evaluate_edited_source_and_keep_an_existing_run_alive()
    {
        using var fixture = new a_real_worker();
        var root = ResolveDirectory(Directory.CreateTempSubdirectory("stage-model-root-spec-"));
        try
        {
            await Evaluate(fixture, root);
        }
        finally { Directory.Delete(root, true); }
    }

    static string ResolveDirectory(DirectoryInfo directory)
    {
        if (directory.ResolveLinkTarget(true) is DirectoryInfo target) return ResolveDirectory(target);
        return directory.Parent is { } parent ? Path.Combine(ResolveDirectory(parent), directory.Name) : directory.FullName;
    }

    static async Task Evaluate(a_real_worker fixture, string root)
    {
        var path = Path.Combine(root, "Model.play");
        var source = validation_body_model.Command("return context.Value > 0;");
        await File.WriteAllTextAsync(path, source);
        var catalog = PersistCatalog(root, source);
        var originalCatalog = await File.ReadAllBytesAsync(catalog);
        var loaded = await SemanticModelLoader.LoadFromPathAsync(path, catalog);
        var bodies = Bodies(loaded);
        var body = bodies.Values.Single();
        var context = new SemanticRuleContext(SemanticValue.Null, SemanticValue.Number(1), "amount", null, null, null);
        await using var execution = new ModelRootBodyExecution(root, new(fixture.WorkerPath, Path.Combine(fixture.CacheDirectory, "units")) { TimeoutMs = 30000, HeapLimitMb = 256 });
        await using var first = await execution.CreateAsync(loaded.Model.Application, bodies, CancellationToken.None);
        (await first.EvaluateAsync(body, context, CancellationToken.None)).Satisfied.ShouldEqual(true);
        await using var second = await execution.CreateAsync(loaded.Model.Application, bodies, CancellationToken.None);
        (await second.EvaluateAsync(body, context, CancellationToken.None)).Satisfied.ShouldEqual(true);
        await File.WriteAllTextAsync(path, validation_body_model.Command("return context.Value < 0;"));
        var edited = await SemanticModelLoader.LoadFromPathAsync(path, catalog);
        var editedBodies = Bodies(edited);
        await using var third = await execution.CreateAsync(edited.Model.Application, editedBodies, CancellationToken.None);
        var result = await third.EvaluateAsync(editedBodies.Values.Single(), context, CancellationToken.None);
        Assert.Null(result.Diagnostic);
        result.Satisfied.ShouldEqual(false);
        (await first.EvaluateAsync(body, context, CancellationToken.None)).Satisfied.ShouldEqual(true);
        Assert.Equal(originalCatalog, await File.ReadAllBytesAsync(catalog));
        AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name.StartsWith("BodyUnit_", StringComparison.Ordinal)).ShouldBeFalse();
    }

    static Dictionary<string, SemanticImplementationBody> Bodies(LoadedSemanticModel loaded) => loaded.ImplementationRequirements.ToDictionary(requirement => requirement.RequirementId, requirement => new SemanticImplementationBody(requirement, loaded.ImplementationContents[requirement.RequirementId]));

    static string PersistCatalog(string root, string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("PinnedApplication"));
        var key = Convert.ToHexString(Encoding.UTF8.GetBytes("Model.play"));
        var document = WorkspaceDocument.Create(catalog.ResolveDocument(key), key, PortablePlayPath.Parse("Model.play"), Encoding.UTF8.GetBytes(source));
        var workspace = ScreenplayWorkspace.Create(catalog.Application, "PinnedApplication", [document], catalog);
        Directory.CreateDirectory(Path.Combine(root, ".screenplay"));
        var path = Path.Combine(root, ".screenplay", PersistedIdentityStateSerializer.FileName);
        File.WriteAllBytes(path, PersistedIdentityStateSerializer.Serialize(PersistedIdentityStateSerializer.From(workspace)));
        return path;
    }
}
