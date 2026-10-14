// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Workspaces;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader.when_loading_release_baseline;

public class with_partial_sources : given.a_release_baseline_tree
{
    void Establish()
    {
        var key = Convert.ToHexString(Encoding.UTF8.GetBytes("other.play"));
        var documents = _workspace.Documents.Add(WorkspaceDocument.Create(_workspace.IdentityCatalog.ResolveDocument(key), key, PortablePlayPath.Parse("other.play"), "concept Other : String"u8));
        var workspace = ScreenplayWorkspace.Create(_workspace.IdentityCatalog.Application, "RecordsApp", documents, _workspace.IdentityCatalog);
        File.WriteAllText(Path.Combine(_root, "other.play"), "concept Other : String");
        _catalogBytes = PersistedIdentityStateSerializer.Serialize(PersistedIdentityStateSerializer.From(workspace));
        File.WriteAllBytes(Path.Combine(_root, ".screenplay", "identities.json"), _catalogBytes);
    }

    async Task Because() => _loaded = await SemanticModelLoader.LoadAsync(_root, ["source.play"], ".screenplay/identities.json", "RecordsApp");
    [Fact] void should_preserve_the_compiler_refusal_of_stale_assignments() => _loaded.Success.ShouldBeFalse();
    [Fact] void should_report_a_compilation_failure() => _loaded.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-PLAN-003");
    [Fact] void should_not_migrate_identities_to_compile_a_subset() => File.ReadAllBytes(Path.Combine(_root, ".screenplay", "identities.json")).ShouldEqual(_catalogBytes);
}
#endif
