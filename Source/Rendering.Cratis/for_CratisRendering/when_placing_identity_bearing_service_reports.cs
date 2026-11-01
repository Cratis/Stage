// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_placing_identity_bearing_service_reports : given.a_source_plan
{
    static readonly string[] SourcePaths = ["context.play", "projects.play", "tasks.play"];
    ArtifactRenderPlan _service = null!;
    ArtifactRenderPlan _rootPlan = null!;
    ArtifactRenderPlan _peer = null!;
    ArtifactRenderPlan _beforeReports = null!;
    ArtifactRenderRequest _serviceRequest = null!;

    async Task Because()
    {
        await BootstrapIdentities();
        _featureSelection = new([PlanSelectionEntry.Feature("Projects", "Registration"), PlanSelectionEntry.Module("Tasks")]);
        _planOptions = _planOptions with { Strict = true };
        _rootPlan = await PlanSources();
        _planOptions = _planOptions with { ServiceLayout = true };
        _service = await PlanSources();
        _planOptions = _planOptions with { ProjectName = "OtherProjects" };
        _peer = await PlanSources();
        _serviceRequest = new(
            _loaded.Model,
            _loaded.Plan,
            CratisRendering.CreateProfile(_planOptions.ApplicationName, new("Projects", _planOptions.RootNamespace) { Strict = true, ServiceLayout = true }),
            _service.Scope)
        {
            AdditionalScopes = _service.AdditionalScopes,
            SemanticIdentities = _loaded.SemanticIdentities,
            IdentityCatalog = _loaded.IdentityCatalog,
            ImplementationRequirements = _loaded.ImplementationRequirements,
            ImplementationContents = _loaded.ImplementationContents,
            TypedContextDescriptors = _loaded.TypedContextDescriptors,
            AttachmentDiagnostics = _loaded.AttachmentDiagnostics
        };
        _beforeReports = new CratisArtifactRenderPlanner().Plan(_serviceRequest with { SemanticIdentities = [], IdentityCatalog = null });
    }

    [Fact]
    void should_place_only_the_two_new_reports_under_the_service_once()
    {
        Assert.NotNull(_loaded.IdentityCatalog);
        Assert.NotEmpty(_loaded.SemanticIdentities);
        Assert.True(_service.Success, string.Join(';', _service.Diagnostics));
        Assert.Equal<string>(["Source/Projects/.stage/completeness.json", "Source/Projects/UNSUPPORTED.md"], Reports(_service).Select(artifact => artifact.RelativePath));
        Assert.All(_service.Artifacts, artifact => Assert.StartsWith("Source/Projects/", artifact.RelativePath, StringComparison.Ordinal));
        Assert.DoesNotContain(_service.Artifacts, artifact => artifact.RelativePath.StartsWith("Source/Projects/Source/", StringComparison.Ordinal));
    }

    [Fact]
    void should_preserve_report_bytes_and_selected_scope_ownership()
    {
        Assert.Equal(2, Reports(_service).Count());
        var expected = ArtifactRenderReports.Create(_service, new(_service.InputVersions!, new([_service.InputVersions!]), [new(_service.Renderer, _service.RendererVersion)]));
        foreach (var report in new[] { expected.Completeness, expected.Unsupported })
        {
            var placed = _service.Artifacts.Single(artifact => artifact.RelativePath == "Source/Projects/" + report.RelativePath);
            Assert.True(report.Bytes.SequenceEqual(placed.Bytes));
            Assert.True(report.Sources.SequenceEqual(placed.Sources));
            Assert.Equal(2, placed.Sources.Length);
            Assert.All(placed.Sources, source => Assert.Contains(new[] { _service.Scope }.Concat(_service.AdditionalScopes), scope => scope.Artifact == source));
        }
    }

    [Fact]
    void should_keep_real_inventory_and_acceptance_in_the_json()
    {
        var report = _service.Artifacts.Single(artifact => artifact.RelativePath == "Source/Projects/.stage/completeness.json");
        using var json = JsonDocument.Parse(report.Bytes.AsMemory());
        var root = json.RootElement;
        Assert.Equal(_service.Strict, root.GetProperty("strict").GetBoolean());
        Assert.Equal(_service.Success, root.GetProperty("success").GetBoolean());
        Assert.Equal(_service.CanPublishArtifacts, root.GetProperty("canPublishArtifacts").GetBoolean());
        Assert.Equal(2, root.GetProperty("scopes").GetArrayLength());
        Assert.NotEmpty(_service.Inventory!.Constructs);
        var constructs = root.GetProperty("inventory").GetProperty("constructs").EnumerateArray().ToArray();
        Assert.Equal(_service.Inventory.Constructs.Select(construct => construct.Identity.ToString()), constructs.Select(construct => construct.GetProperty("identity").GetString()));
        Assert.All(constructs, construct => Assert.Equal(JsonValueKind.Null, construct.GetProperty("modelPath").ValueKind));
    }

    [Fact]
    void should_leave_existing_code_sources_bytes_and_acceptance_unchanged()
    {
        var code = _service.Artifacts.Except(Reports(_service)).ToArray();
        Assert.NotEmpty(code);
        Assert.Equal(_beforeReports.Artifacts.Select(artifact => artifact.RelativePath), code.Select(artifact => artifact.RelativePath));
        foreach (var artifact in _beforeReports.Artifacts)
        {
            var retained = code.Single(candidate => candidate.RelativePath == artifact.RelativePath);
            Assert.True(artifact.Bytes.SequenceEqual(retained.Bytes));
            Assert.True(artifact.Sources.SequenceEqual(retained.Sources));
        }
        Assert.Equal(_beforeReports.Strict, _service.Strict);
        Assert.Equal(_beforeReports.Success, _service.Success);
        Assert.Equal(_beforeReports.Outcome, _service.Outcome);
        Assert.Equal(_beforeReports.CanPublishArtifacts, _service.CanPublishArtifacts);
        Assert.Equal(_beforeReports.Degradations, _service.Degradations);
        Assert.Equal(_beforeReports.PlanEvidence, _service.PlanEvidence);
    }

    [Fact]
    void should_keep_non_service_root_reports_byte_for_byte()
    {
        Assert.Equal<string>([".stage/completeness.json", "UNSUPPORTED.md"], Reports(_rootPlan).Select(artifact => artifact.RelativePath));
        foreach (var root in Reports(_rootPlan))
        {
            var placed = _service.Artifacts.Single(artifact => artifact.RelativePath == "Source/Projects/" + root.RelativePath);
            Assert.True(root.Bytes.SequenceEqual(placed.Bytes));
            Assert.True(root.Sources.SequenceEqual(placed.Sources));
        }
    }

    [Fact]
    void should_give_two_services_disjoint_report_paths()
    {
        Assert.Equal<string>(["Source/OtherProjects/.stage/completeness.json", "Source/OtherProjects/UNSUPPORTED.md"], Reports(_peer).Select(artifact => artifact.RelativePath));
        Assert.Empty(Reports(_service).Select(artifact => artifact.RelativePath).Intersect(Reports(_peer).Select(artifact => artifact.RelativePath), StringComparer.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("Source/Projects/.stage/completeness.json", "An artifact render plan contains a duplicate relative path.")]
    [InlineData("Source/Projects/UNSUPPORTED.md", "An artifact render plan contains a duplicate relative path.")]
    [InlineData("source/projects/.STAGE/COMPLETENESS.JSON", "An artifact render plan contains paths that collide on a case-insensitive file system.")]
    [InlineData("source/projects/unsupported.MD", "An artifact render plan contains paths that collide on a case-insensitive file system.")]
    void should_still_refuse_same_service_report_collisions(string path, string message)
    {
        var occupied = ArtifactRenderPlan.Create(_serviceRequest, [.. _beforeReports.Artifacts, PlannedArtifact.CreateText(path, "occupied\n")], _beforeReports.Diagnostics);
        var exception = Assert.Throws<InvalidArtifactRenderContract>(() => CratisPlanReports.Finalize(_serviceRequest, occupied, new("Projects", _planOptions.RootNamespace)));
        Assert.Equal(message, exception.Message);
    }

    async Task BootstrapIdentities()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create(_planOptions.ApplicationName));
        var documents = SourcePaths.Order(StringComparer.Ordinal).Select(path =>
        {
            var key = Convert.ToHexString(Encoding.UTF8.GetBytes(path));
            return WorkspaceDocument.Create(catalog.ResolveDocument(key), key, PortablePlayPath.Parse(path), File.ReadAllBytes(Path.Combine(_root, path)));
        }).ToArray();
        var workspace = ScreenplayWorkspace.Create(catalog.Application, _planOptions.ApplicationName, [.. documents], catalog);
        Assert.True(workspace.Compilation.Success, string.Join(';', workspace.Compilation.Diagnostics));
        Assert.NotEmpty(workspace.IdentityCatalog.Semantics);
        Directory.CreateDirectory(Path.Combine(_root, ".screenplay"));
        await File.WriteAllBytesAsync(Path.Combine(_root, ".screenplay", "identities.json"), PersistedIdentityStateSerializer.Serialize(PersistedIdentityStateSerializer.From(workspace)));
        var loaded = await SemanticModelLoader.LoadAsync(_root, [.. SourcePaths], ".screenplay/identities.json", _planOptions.ApplicationName);
        Assert.True(loaded.Success, string.Join(';', loaded.Diagnostics));
        _loaded = loaded.Loaded!;
        Assert.True(_loaded.SemanticIdentities.SequenceEqual(workspace.IdentityCatalog.Semantics));
        Assert.Equal(workspace.Compilation.Value!.Model.Revision, _loaded.Model.Revision);
    }

    async Task<ArtifactRenderPlan> PlanSources()
    {
        var result = await CratisRendering.PlanFrom(new PlaySources(_root, [.. SourcePaths], ".screenplay/identities.json"), _featureSelection, _planOptions);
        Assert.True(result.Success, string.Join(';', result.Diagnostics));
        Assert.NotNull(result.Plan);
        return result.Plan;
    }

    static IEnumerable<PlannedArtifact> Reports(ArtifactRenderPlan plan) => plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".stage/completeness.json", StringComparison.Ordinal) || artifact.RelativePath.EndsWith("UNSUPPORTED.md", StringComparison.Ordinal));
}
